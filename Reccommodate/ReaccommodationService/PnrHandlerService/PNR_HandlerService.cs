using Newtonsoft.Json;
using OfficeOpenXml;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.UCGHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class PNR_HandlerService : IPNR_HandlerService
    {

        private readonly ILogHelper _logHelper;
        private readonly string _caller = typeof(ReaccommodationHandlerService).Name;
        private readonly IDataMsService _dataMsService;
        private readonly IUCGWebServices _uCGWebService;
        private readonly ISalesForceService _salesForceService;
        private readonly INotificationMS  _notificationMS;
        public PNR_HandlerService(ILogHelper logHelper, IDataMsService dataMsService, IUCGWebServices uCGWebServices, ISalesForceService salesForceService, INotificationMS notificationMS) {        

            _logHelper = logHelper; 
            _dataMsService = dataMsService; 
            _uCGWebService = uCGWebServices;    
            _salesForceService = salesForceService; 
            _notificationMS = notificationMS;   
        } 
        public async Task<string> SaveDataAndNotify(PNRDetails pNRDetails, DisruptedFlightDB? disruptedFlight, Dictionary<string, string>? AppParameterList, NhbTemplateResponse nhbTemplate)
        {
            try
            {
                if (pNRDetails != null && pNRDetails.routingDetail != null && pNRDetails.pnrDetail != null)
                {
                    List<PNRDetail> MovePNR = pNRDetails.pnrDetail.Where(x => x.RECOStatus == Enum_PNR.Success.ToString()).ToList();
                    List<PNRDetail> notMovedPNR = pNRDetails.pnrDetail.Where(x => x.RECOStatus != Enum_PNR.Success.ToString()).ToList();

                    UpdateDisruptedFlights udpatedisruptedFlights = await UpdateModelDisruptedFlights(disruptedFlight, MovePNR.Count(), notMovedPNR.Count());
                    
                    var T1UpdatePNR = Task.Run(async () => await _dataMsService.UpdatePNRDetails(pNRDetails.pnrDetail));
                    await Task.Delay(1000);
                    var T2UpdateDisrupted = Task.Run(async () => await _dataMsService.UpdateDisruptedflights(udpatedisruptedFlights));
                    var T3PostRouting = Task.Run(async () => await _dataMsService.PostRoutingDetails(pNRDetails.routingDetail));
                   // var _SalesForce = Task.Run(async () => await incidentCreatedBySalesForce(notMovedPNR));
                    await Task.WhenAll(T1UpdatePNR, T2UpdateDisrupted, T3PostRouting);
                    await _logHelper.LogInfo($"{_caller}:{"SaveDataAndNotify"} :- {" ucgNotification Start"}");
                    bool _UCGStatus = await ucgNotification(pNRDetails, disruptedFlight, udpatedisruptedFlights, MovePNR, notMovedPNR, AppParameterList, nhbTemplate);
                    await _logHelper.LogInfo($"{_caller}:{"SaveDataAndNotify"} :- {" ucgNotification End"}{_UCGStatus}");

                    return "true";
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{":- SaveDataAndNotify"} :- {"End Flight No :"}{disruptedFlight?.FlightNumber ?? ""}");
                    return "No PNR are available";
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"SaveDataAndNotify"}{" msg:-"} {ex.Message}:- {":- End :- "}{JsonConvert.SerializeObject(disruptedFlight)}");
                return ex.Message;
            }
        }
        private async Task<bool> ucgNotification(PNRDetails pNRDetails, DisruptedFlightDB disruptedFlight, UpdateDisruptedFlights udpatedisruptedFlights , List<PNRDetail> MovePNR, List<PNRDetail> notMovedPNR, Dictionary<string, string>? AppParameterList, NhbTemplateResponse nhbTemplate)
        {
            try
            {
                string MessagePathway = AppParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                {
                    //string PNRDetails = @"Configs\PNRDetails.xlsx";
                    string PNRDetails = @"/app/Configs/PNRDetails.xlsx";  //OCP Server
                    string Excelstring = await CreateExcel(pNRDetails, disruptedFlight, PNRDetails);
                    await _uCGWebService.sendTheEmailByNHub(disruptedFlight, nhbTemplate,Enum_Template.Reaccommodation_Completed.ToString(), PNRDetails, "");
                    List<PNRDetail> NavitaireFailurePNR = pNRDetails.pnrDetail.Where(x => x.ReasonForFailure != Enum_DisruptionType.Completed.ToString()).ToList();
                    if (NavitaireFailurePNR.Count > 0)
                    {
                        //string NonActionPNR = @"Configs\NonActionPNR.xlsx";
                        string NonActionPNR = @"/app/Configs/NonActionPNR.xlsx";  //OCP Server
                        string Contact = await FailedPNRCreateExcel(NavitaireFailurePNR, NonActionPNR);
                        await _uCGWebService.sendTheEmailByNHub(disruptedFlight, nhbTemplate, Enum_Template.PNRs_Not_Reaccommodated.ToString(), NonActionPNR, "");
                    }
                }
                else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                {
                    string Excelstring = await CreateExcel(pNRDetails, disruptedFlight, "");
                    string emailTemplate = await _uCGWebService.generateEmailTemplateAsync(disruptedFlight, Excelstring, MovePNR.Count(), notMovedPNR.Count());

                    await _uCGWebService.sendTheEmailAsync(emailTemplate,"");

                    List<PNRDetail> NavitaireFailurePNR = pNRDetails.pnrDetail.Where(x => x.ReasonForFailure != Enum_DisruptionType.Completed.ToString()).ToList();
                    if (NavitaireFailurePNR.Count > 0)
                    {
                        string message = @"
Hi Team,

These are the given PNRs that are not reaccommodated. attached the PNR Details Excel file
";
                        string Contact = await FailedPNRCreateExcel(NavitaireFailurePNR,"");
                        string FailedPNR = await _uCGWebService.NavitaireFailurePNRTemplateAsync(disruptedFlight, Contact, message);
                        await _uCGWebService.sendTheEmailAsync(FailedPNR,"");
                    }
                }
                disruptedFlight.TotalPnrCounts = udpatedisruptedFlights.ReaccommodatedPNRCount + udpatedisruptedFlights.NotReaccommodatedPNRCount;
                disruptedFlight.ReaccommodatedPNRCount = udpatedisruptedFlights.ReaccommodatedPNRCount;
                disruptedFlight.NotReaccommodatedPNRCount= udpatedisruptedFlights.NotReaccommodatedPNRCount;
                bool SendNotificationstatus = await _notificationMS.fnSendNotification(disruptedFlight);
                return true;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{":- ucgNotification"}{" msg:-"} {ex.Message}:- {":- End "}{JsonConvert.SerializeObject(disruptedFlight)}");
                return false;   
            }
        }
        private async Task<string> incidentCreatedBySalesForce(List<PNRDetail> pNRTableModels)
        {
            try
            {
                if (pNRTableModels.Count() > 0)
                {
                    string _status = "false";
                    string Token = await _salesForceService.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(Token))
                    {
                        for (int i = 0; i < pNRTableModels.Count; i++)
                        {
                            await _salesForceService.ExecuteCompositeRequestInternalAsync(pNRTableModels[i], Token);
                        }
                    }
                    return "true";
                }
                else
                {
                    await _logHelper.LogInfo($"{_caller}:{"incidentCreatedBySalesForce"} :- {"ALL the PNR are moved"}");
                    return "ALL the PNR are moved";
                }

            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- incidentCreatedBySalesForce"}{" msg:-"} {ex.Message}:- {"End "}");
                return ex.Message;
            }
        }
        private async Task<UpdateDisruptedFlights> UpdateModelDisruptedFlights(DisruptedFlightDB _disruptedFlightsModelResponse, int MovePNRCount, int NotMovePNRCount)
        {
            if (_disruptedFlightsModelResponse != null)
            {
                _disruptedFlightsModelResponse.ReaccommodatedPNRCount = _disruptedFlightsModelResponse.ReaccommodatedPNRCount + MovePNRCount;
            }
            UpdateDisruptedFlights disruptedFlightsModelResponse = new UpdateDisruptedFlights
            {
                FLTID = _disruptedFlightsModelResponse.FLTID,
                AirlineCode = _disruptedFlightsModelResponse.AirlineCode,
                FlightNumber = _disruptedFlightsModelResponse.FlightNumber,
                FlightDate = _disruptedFlightsModelResponse.FlightDate,
                Origin = _disruptedFlightsModelResponse.Origin,
                Destination = _disruptedFlightsModelResponse.Destination,
                STD = _disruptedFlightsModelResponse.STD,
                ETD = _disruptedFlightsModelResponse.ETD,
                STA = _disruptedFlightsModelResponse.STA,
                ETA = _disruptedFlightsModelResponse.ETA,
                DisruptionType = _disruptedFlightsModelResponse.DisruptionType,
                RECOStatus = Enum_DisruptionType.Completed.ToString(),
                IsApproved = _disruptedFlightsModelResponse.IsApproved,
                Retry = _disruptedFlightsModelResponse.Retry,
                Source = _disruptedFlightsModelResponse.Source,
                ReasonForNotProcessing = Enum_DisruptionType.Processedsuccessfully.ToString(),
                ReaccommodatedPNRCount = _disruptedFlightsModelResponse.ReaccommodatedPNRCount,
                DisruptionCode = _disruptedFlightsModelResponse.DisruptionCode, 
                NotReaccommodatedPNRCount = NotMovePNRCount,
                IsDelayProcess=true,    
            };
            return disruptedFlightsModelResponse;
        }
        private async Task<string> CreateExcel(PNRDetails pNRDetails, DisruptedFlightDB disruptedFlight,string filePath)
        {
            try
            {
                List<DisruptedFlightDB> disrupteds = new List<DisruptedFlightDB> { disruptedFlight };
                using (ExcelPackage package = new ExcelPackage())
                {
                    // Flight Details Sheet
                    var flightDetailsSheet = package.Workbook.Worksheets.Add("Flight Details");

                    // Adding headers for Flight Details
                    flightDetailsSheet.Cells["A1"].Value = "Flight Details";

                    var headers = new[] { "Flight No.", "Origin", "Destination", "DisruptionType", "STD", "STA", "ETD", "ETA", "Reco Status", "CreatedBy" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        flightDetailsSheet.Cells[3 + i, 1].Value = headers[i];
                    }

                    // Adding data for Flight Details
                    flightDetailsSheet.Cells["B3"].Value = Convert.ToInt32(disruptedFlight.FlightNumber);
                    flightDetailsSheet.Cells["B4"].Value = disruptedFlight.Origin;
                    flightDetailsSheet.Cells["B5"].Value = disruptedFlight.Destination;
                    flightDetailsSheet.Cells["B6"].Value = disruptedFlight.DisruptionType;
                    flightDetailsSheet.Cells["B7"].Value = disruptedFlight.STD !=null ? (disruptedFlight.STD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;
                    flightDetailsSheet.Cells["B8"].Value = disruptedFlight.STA !=null ? (disruptedFlight.STA ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss"):null;
                    flightDetailsSheet.Cells["B9"].Value = disruptedFlight.ETD !=null ?(disruptedFlight.ETD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss"):null;
                    flightDetailsSheet.Cells["B10"].Value = disruptedFlight.ETD !=null? (disruptedFlight.ETA ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;
                    flightDetailsSheet.Cells["B11"].Value = "Completed";
                    flightDetailsSheet.Cells["B12"].Value = disruptedFlight.CreatedBy;

                    // PNR List Sheet
                    var pnrListSheet = package.Workbook.Worksheets.Add("PNR Details");
                    pnrListSheet.Cells["A1"].Value = "PNR List";

                    pnrListSheet.Cells["B1"].Value = "Total PNR Count : " + pNRDetails?.pnrDetail?.Count();
                    pnrListSheet.Cells["C1"].Value = "Re-accommodated PNR Count : " + pNRDetails?.pnrDetail?.Where(x => x.ReasonForFailure == "Completed").Count();
                    pnrListSheet.Cells["D1"].Value = "Not Re-accommodated PNR Count : " + pNRDetails?.pnrDetail?.Where(x => x.ReasonForFailure != "Completed" && x.ReasonForFailure != "restrictedSSR").Count();
                    pnrListSheet.Cells["E1"].Value = "Restricted PNR Count : " + pNRDetails?.pnrDetail?.Where(x => x.ReasonForFailure == "restrictedSSR").Count();

                    // Adding headers for PNR details
                    var pnrHeaders = new[] { "PNR", "Reco status", "Remarks" };
                    for (int i = 0; i < pnrHeaders.Length; i++)
                    {
                        pnrListSheet.Cells[3, i + 1].Value = pnrHeaders[i];
                    }

                    int rowIndex = 4;
                    List<string> customOrder = new List<string> { "restrictedSSR", "NoFlightAvailable", "Completed" };
                    List<PNRDetail> pNRs = pNRDetails?.pnrDetail.OrderBy(pnr => customOrder.IndexOf(pnr.ReasonForFailure)).ToList();
                    foreach (var pnr in pNRs)
                    {
                        pnrListSheet.Cells["A" + rowIndex].Value = pnr.PNRCode;
                        pnrListSheet.Cells["B" + rowIndex].Value = pnr.RECOStatus;
                        pnrListSheet.Cells["C" + rowIndex].Value = pnr.ReasonForFailure;
                        rowIndex++;

                        var subHeaders = new[] { "Flight No", "Flight Date", "Flight Booking", "Origin", "Destination" };
                        for (int i = 0; i < subHeaders.Length; i++)
                        {
                            pnrListSheet.Cells[rowIndex, i + 2].Value = subHeaders[i];
                        }

                        rowIndex++;
                        foreach (var routing in pNRDetails?.routingDetail?.Where(x => x.PNRID == pnr.PNRID))
                        {
                            pnrListSheet.Cells["B" + rowIndex].Value = Convert.ToInt32(routing.FlightNumber);
                            pnrListSheet.Cells["C" + rowIndex].Value = (routing.STD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss");
                            pnrListSheet.Cells["D" + rowIndex].Value = routing.FlightBooking;
                            pnrListSheet.Cells["E" + rowIndex].Value = routing.Origin;
                            pnrListSheet.Cells["F" + rowIndex].Value = routing.Destination;

                            rowIndex++;
                        }

                        rowIndex++; // Skip a row for the next PNR
                    }


                    if (!(string.IsNullOrEmpty(filePath)))
                    {
                        await _logHelper.LogInfo("1.Start CreateExcel : "+ filePath);
                        FileInfo fileInfo = new FileInfo(filePath);
                        using (ExcelPackage excepackage = fileInfo.Exists ? new ExcelPackage(fileInfo) : new ExcelPackage())
                        {
                            await _logHelper.LogInfo("1.start clearing and writing Excel file");
                            foreach (var worksheet in excepackage.Workbook.Worksheets)
                            {
                                worksheet.Cells.Clear();
                            }
                        }
                        package.SaveAs(new FileInfo(filePath));
                    }
                    // Save the Excel file to a MemoryStream
                    using (var memoryStream = new MemoryStream())
                    {
                        package.SaveAs(memoryStream);
                        byte[] fileBytes = memoryStream.ToArray();
                        return Convert.ToBase64String(fileBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- CreateExcel"}{" msg:-"} {ex.Message}:- {":- End "}{JsonConvert.SerializeObject(disruptedFlight)}");
                return "";
            }
        }
        private async Task<string> FailedPNRCreateExcel(List<PNRDetail> pNRDetails,string filePath)
        {
            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    var pnrListSheet = package.Workbook.Worksheets.Add("PNR Details");
                    pnrListSheet.Cells["A1"].LoadFromCollection(pNRDetails, true);
                    var cells = pnrListSheet.Cells[pnrListSheet.Dimension.Address];
                    if(!(string.IsNullOrEmpty(filePath)))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        using (ExcelPackage excepackage = fileInfo.Exists ? new ExcelPackage(fileInfo) : new ExcelPackage())
                        {
                            foreach (var worksheet in excepackage.Workbook.Worksheets)
                            {
                                worksheet.Cells.Clear();
                            }
                        }
                        package.SaveAs(new FileInfo(filePath));
                    }
                    using (var memoryStream = new MemoryStream())
                    {
                        package.SaveAs(memoryStream);
                        byte[] fileBytes = memoryStream.ToArray();
                        return Convert.ToBase64String(fileBytes);
                    }
                }

            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- FailedPNRCreateExcel"}{" msg:-"} {ex.Message}:- {":- End "}{JsonConvert.SerializeObject(pNRDetails)}");
                return "";
            }
        }
    }
}
