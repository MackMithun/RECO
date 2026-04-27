using AutoMapper;
using Newtonsoft.Json;
using OfficeOpenXml;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.NHub;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.UCGHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class DashboardDetailsPerPNR : IDashboardDetailsPerPNR
    {
        private readonly ILogHelper _logHelper;
        private readonly IBookingService _bookingService;
        private readonly IUCGWebServices _uCGWebService;
        private readonly INotificationTemplateService _notificationTemplateService;
        private readonly INotificationHUBService _notificationHUBService;
        private readonly string _caller = typeof(DashboardDetailsPerPNR).Name;

        public DashboardDetailsPerPNR(ILogHelper logHelper, IMapper mapper, IBookingService bookingService, IUCGWebServices uCGWebServices, INotificationTemplateService notificationTemplateService, INotificationHUBService notificationHUBService)
        {
            _bookingService = bookingService;
            _logHelper = logHelper;
            _uCGWebService = uCGWebServices;
            _notificationTemplateService = notificationTemplateService; 
            _notificationHUBService = notificationHUBService;       
        }
        public async Task<bool> fnGenrateDashboardPerPNR(DashboardDetails disruptedFlight,FetchDashboardResponse fetchDashboardResponse, Dictionary<string, string> AppParameterList, NhbTemplateResponse nhbTemplate, string Token)
        {
            try
            {
                bool _status = false;
                string PNRDetails = "";
                string MessagePathway = AppParameterList[Enum_MessagePathway.MessagePathway.ToString()].ToLower().Trim();
                if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                {
                    //PNRDetails = @"Configs\DashboardDetails.xlsx";
                    PNRDetails = @"/app/Configs/DashboardDetails.xlsx";  //OCP Server
                }
                string ExcelBase64 =await CreatehDashboardExcel(fetchDashboardResponse, Token, PNRDetails);
                if(!string.IsNullOrEmpty(ExcelBase64))
                {
                    if (MessagePathway == Enum_MessagePathway.NhubMode.ToString().ToLower().Trim())
                    {
                        NhubTemplateJSON nhubTemplateJSON = await _notificationTemplateService.fnGenrateDashboardTemplateJSON(disruptedFlight, nhbTemplate);
                        notificationTemplateHUB? notificationTemplateHUB = JsonConvert.DeserializeObject<notificationTemplateHUB>(nhubTemplateJSON.TemplateJSON);
                        NotificationHUBResponse notificationHUBResponse = await _notificationHUBService.SendTheNotify(notificationTemplateHUB, nhubTemplateJSON.ID, PNRDetails);
                        if (!string.IsNullOrEmpty(notificationHUBResponse.RequestId))
                        {
                            await _logHelper.LogInfo("fnGenrateDashboardPerPNR RequestId :- " + notificationHUBResponse.RequestId +" Date :-"+ disruptedFlight?.startDate+" - "+ disruptedFlight?.endDate);
                            return true;
                        }
                    }
                    else if (MessagePathway == Enum_MessagePathway.UcgMode.ToString().ToLower().Trim())
                    {
                        await _logHelper.LogInfo($"{_caller}{"fnGenrateDashboardPerPNR"}{"Email : "}{disruptedFlight.EmailID}");
                        string emailTemplate = await _uCGWebService.generateDashboardEmailTemplateAsync(disruptedFlight, ExcelBase64);
                        await _uCGWebService.sendTheEmailAsync(emailTemplate, disruptedFlight.EmailID);
                    }
                     _status =true;
                }
                return _status;
            }
            catch(Exception ex)
            {
                await _logHelper.LogError($"{_caller}{"fnGenrateDashboardPerPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                return false;
            }
        }
        private async Task<string> CreatehDashboardExcel(FetchDashboardResponse fetchDashboardResponse, string Token,string filePath)
        {
            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    if (fetchDashboardResponse.PnrReaccommodated != null && fetchDashboardResponse.PnrReaccommodated.PNRDetailReportItem.Count() > 0)
                    {
                        int k = 2;
                        var PNRReaccommodated = package.Workbook.Worksheets.Add(Enum_Dashboard.PNR_Reaccommodated.ToString());
                        var headers = Enum_Dashboard.PNR_Reaccommodated.GetDescription().Split(",").ToArray();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            PNRReaccommodated.Cells[k, i + 1].Value = headers[i];
                        }
                        foreach (var pnrReaccommodate in fetchDashboardResponse.PnrReaccommodated.PNRDetailReportItem)
                        {
                            try
                            {
                                k++;
                                var disruptedFlightNumbers = string.Join(", ", pnrReaccommodate.RoutingReport.Where(x => x.FlightBooking?.ToLower().Trim() == "disrupted")
                                .Select(x => x.FlightNumber ?? string.Empty));
                                var alternateFlightNumbers = string.Join(", ", pnrReaccommodate.RoutingReport.Where(x => x.FlightBooking?.ToLower().Trim() == "alternate")
                                    .Select(x => x.FlightNumber ?? string.Empty));
                                RoutingDetail Disrupted_Flight = pnrReaccommodate.RoutingReport.FirstOrDefault(x => x.FlightBooking?.ToLower().Trim() == "disrupted") ?? new RoutingDetail();
                                RoutingDetail alternateFlight = pnrReaccommodate.RoutingReport.FirstOrDefault(x => x.FlightBooking?.ToLower().Trim() == "alternate") ?? new RoutingDetail();
                                var bookingDetails = "";
                                Journey? journey = new Journey();
                                decimal amount = 0;
                                decimal PNR_Amount = 0;
                                int attempts = 0;
                                bool success = false;
                                while (attempts < 2 && !success)
                                {
                                    try
                                    {
                                        bookingDetails = await _bookingService.GetBookingByRecordLocator(pnrReaccommodate.PNRCode, Token);
                                        if (!string.IsNullOrEmpty(bookingDetails))
                                        {
                                            BookingDetails? booking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                                            if (booking != null && booking.Data != null && booking?.Data?.Payments != null && booking?.Data?.Payments.Count() > 0)
                                            {
                                                amount = await CheckNegativeAmounts(booking);
                                                success = true;
                                            }
                                            if (booking != null && booking.Data != null && booking?.Data?.Journeys != null && booking?.Data?.Journeys.Count() > 0)
                                            {
                                                journey = booking?.Data?.Journeys?.FirstOrDefault(x => x.Designator?.Origin?.ToUpper().Trim() == Disrupted_Flight?.Origin?.ToUpper().Trim());
                                                success = true;
                                            }
                                            if (booking != null && booking.Data != null && booking?.Data?.Breakdown != null)
                                            {
                                                PNR_Amount = booking.Data.Breakdown.TotalAmount;
                                            }
                                        }
                                        if (success)
                                        {
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await _logHelper.LogError("CreatehDashboardExcelperPNR : PNR :- " + pnrReaccommodate.PNRCode + " :-msg:- " + ex.Message);
                                    }
                                    attempts++;
                                }
                                double totalHours = (Disrupted_Flight.STD.Value - TimeZoneService.convertToIND(pnrReaccommodate.CreatedOn.Value)).TotalHours;
                                int hours = (int)Math.Floor(totalHours);
                                double fractionalPart = totalHours - hours;
                                double minutes = Math.Round(fractionalPart * 60, 2);
                                var Current_FlightNo = string.Join(",", journey?.Segments?.Select(x => x.Identifier?.Identifier?.ToString() ?? string.Empty) ?? Enumerable.Empty<string>());
                                PNRReaccommodated.Cells[$"A{k}"].Value = pnrReaccommodate.PNRCode;
                                PNRReaccommodated.Cells[$"B{k}"].Value = disruptedFlightNumbers;
                                PNRReaccommodated.Cells[$"C{k}"].Value = Disrupted_Flight.STD != null ? (Disrupted_Flight.STD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;
                                PNRReaccommodated.Cells[$"D{k}"].Value = alternateFlightNumbers;
                                PNRReaccommodated.Cells[$"E{k}"].Value = alternateFlight.STD != null ? (alternateFlight.STD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;
                                PNRReaccommodated.Cells[$"F{k}"].Value = Current_FlightNo;
                                PNRReaccommodated.Cells[$"G{k}"].Value = (journey?.Segments != null && journey.Segments.Count > 0 && journey.Segments[0].Designator?.Departure != null) ? journey?.Segments?[0].Designator?.Departure.Value.ToString("dd-MM-yyyy HH:mm:ss") : null;
                                PNRReaccommodated.Cells[$"H{k}"].Value = CompareFlightNumbers(alternateFlightNumbers, Current_FlightNo);
                                PNRReaccommodated.Cells[$"I{k}"].Value = amount < 0 ? "YES" : "NO";
                                PNRReaccommodated.Cells[$"J{k}"].Value = amount < 0 ? amount.ToString() : " ";
                                PNRReaccommodated.Cells[$"K{k}"].Value = PNR_Amount;
                                PNRReaccommodated.Cells[$"L{k}"].Value = (Disrupted_Flight?.STD.HasValue == true && pnrReaccommodate?.CreatedOn.HasValue == true)
                                    ? $"{hours}.{minutes:00} hr" : (double?)null;
                                PNRReaccommodated.Cells[$"M{k}"].Value = pnrReaccommodate?.CreatedOn != null ? TimeZoneService.convertToIND(pnrReaccommodate?.CreatedOn.Value ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;

                            }
                            catch
                            {
                                continue;
                            }
                           
                        }
                    }
                    if (fetchDashboardResponse.PnrNotReaccommodated != null && fetchDashboardResponse.PnrNotReaccommodated.PNRDetailReportItem.Count() > 0)
                    {
                        int k = 2;
                        var NotPNRReaccommodated = package.Workbook.Worksheets.Add(Enum_Dashboard.PNR_Not_Reaccommodated.ToString() );
                        var headers =  Enum_Dashboard.PNR_Not_Reaccommodated.GetDescription().Split(",").ToArray();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            NotPNRReaccommodated.Cells[k, i + 1].Value = headers[i];
                        }
                        foreach (var pnrNotReaccommodate in fetchDashboardResponse.PnrNotReaccommodated.PNRDetailReportItem)
                        {
                            try
                            {
                                k++;
                                var disruptedFlightNumbers = string.Join(", ", pnrNotReaccommodate.RoutingReport?.Where(x => x.FlightBooking?.ToLower().Trim() == "disrupted")
                                   .Select(x => x.FlightNumber ?? string.Empty) ?? Enumerable.Empty<string>());
                                RoutingDetail Disrupted_Flight = pnrNotReaccommodate.RoutingReport?.FirstOrDefault(x => x.FlightBooking?.ToLower().Trim() == "disrupted") ?? new RoutingDetail();
                                var bookingDetails = "";
                                Journey? journey = new Journey();
                                decimal amount = 0;
                                decimal PNR_Amount = 0;
                                int attempts = 0;
                                bool success = false;
                                while (attempts < 2 && !success)
                                {
                                    try
                                    {
                                        bookingDetails = await _bookingService.GetBookingByRecordLocator(pnrNotReaccommodate.PNRCode, Token);
                                        if (!string.IsNullOrEmpty(bookingDetails))
                                        {
                                            BookingDetails? booking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                                            if (booking != null && booking.Data != null && booking?.Data?.Journeys != null && booking?.Data?.Journeys.Count() > 0)
                                            {
                                                amount = await CheckNegativeAmounts(booking);
                                                journey = booking?.Data?.Journeys?.FirstOrDefault(x => x.Designator?.Origin?.ToUpper().Trim() == Disrupted_Flight?.Origin?.ToUpper().Trim());
                                                success = true;
                                            }
                                            if (booking != null && booking.Data != null && booking?.Data?.Breakdown != null)
                                            {
                                                PNR_Amount = booking.Data.Breakdown.TotalAmount;
                                            }
                                        }
                                        if (success)
                                        {
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await _logHelper.LogError("CreatehDashboardExcelperPNR : PNR :- " + pnrNotReaccommodate.PNRCode + " :-msg:- " + ex.Message);
                                    }
                                    attempts++;
                                }
                                double totalHours = (Disrupted_Flight.STD.Value - TimeZoneService.convertToIND(pnrNotReaccommodate.CreatedOn.Value)).TotalHours;
                                int hours = (int)Math.Floor(totalHours);
                                double fractionalPart = totalHours - hours;
                                double minutes = Math.Round(fractionalPart * 60, 2);
                                var Current_FlightNo = string.Join(",", journey?.Segments?.Select(x => x.Identifier?.Identifier?.ToString() ?? string.Empty) ?? Enumerable.Empty<string>());
                                NotPNRReaccommodated.Cells[$"A{k}"].Value = pnrNotReaccommodate.PNRCode;
                                NotPNRReaccommodated.Cells[$"B{k}"].Value = disruptedFlightNumbers;
                                NotPNRReaccommodated.Cells[$"C{k}"].Value = Disrupted_Flight.STD != null ? (Disrupted_Flight.STD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;
                                NotPNRReaccommodated.Cells[$"D{k}"].Value = Current_FlightNo;
                                NotPNRReaccommodated.Cells[$"E{k}"].Value = (journey?.Segments != null && journey.Segments.Count > 0 && journey.Segments[0].Designator?.Departure != null) ? journey?.Segments?[0].Designator?.Departure.Value.ToString("dd-MM-yyyy HH:mm:ss") : null;
                                NotPNRReaccommodated.Cells[$"F{k}"].Value = CompareFlightNumbers(disruptedFlightNumbers, Current_FlightNo);
                                NotPNRReaccommodated.Cells[$"G{k}"].Value = amount < 0 ? "YES" : "NO";
                                NotPNRReaccommodated.Cells[$"H{k}"].Value = amount < 0 ? amount : " ";
                                NotPNRReaccommodated.Cells[$"I{k}"].Value = PNR_Amount;
                                NotPNRReaccommodated.Cells[$"J{k}"].Value = (Disrupted_Flight?.STD.HasValue == true && pnrNotReaccommodate?.CreatedOn.HasValue == true)
                                    ? $"{hours}.{minutes:00} hr" : (double?)null;
                                NotPNRReaccommodated.Cells[$"K{k}"].Value = pnrNotReaccommodate?.CreatedOn != null ? TimeZoneService.convertToIND(pnrNotReaccommodate?.CreatedOn.Value ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;

                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                    if (fetchDashboardResponse.PnrNoAction != null && fetchDashboardResponse.PnrNoAction.PNRDetailReportItem.Count() > 0)
                    {
                        int k = 2;
                        var NoPNRAction = package.Workbook.Worksheets.Add(Enum_Dashboard.No_PNR_Action.ToString());
                        var headers = Enum_Dashboard.No_PNR_Action.GetDescription().Split(",").ToArray();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            NoPNRAction.Cells[k, i + 1].Value = headers[i];
                        }
                        foreach (var noActionPNR in fetchDashboardResponse.PnrNoAction.PNRDetailReportItem)
                        {
                            try
                            {
                                k++;
                                var disruptedFlightNumbers = string.Join(", ", noActionPNR.RoutingReport.Where(x => x.FlightBooking?.ToLower().Trim() == "disrupted")
                                   .Select(x => x.FlightNumber ?? string.Empty));
                                RoutingDetail Disrupted_Flight = noActionPNR.RoutingReport.FirstOrDefault(x => x.FlightBooking?.ToLower().Trim() == "disrupted") ?? new RoutingDetail();
                                var bookingDetails = "";
                                Journey? journey = new Journey();
                                decimal amount = 0;
                                int attempts = 0;
                                bool success = false;
                                decimal PNR_Amount = 0;
                                while (attempts < 2 && !success)
                                {
                                    try
                                    {
                                        bookingDetails = await _bookingService.GetBookingByRecordLocator(noActionPNR.PNRCode, Token);
                                        if (!string.IsNullOrEmpty(bookingDetails))
                                        {
                                            BookingDetails? booking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                                            if (booking != null && booking.Data != null && booking?.Data?.Journeys != null && booking?.Data?.Journeys.Count() > 0)
                                            {
                                                amount = await CheckNegativeAmounts(booking);
                                                journey = booking?.Data?.Journeys?.FirstOrDefault(x => x.Designator?.Origin?.ToUpper().Trim() == Disrupted_Flight?.Origin?.ToUpper().Trim());
                                                success = true;
                                            }
                                            if (booking != null && booking.Data != null && booking?.Data?.Breakdown != null)
                                            {
                                                PNR_Amount = booking.Data.Breakdown.TotalAmount;
                                            }
                                        }
                                        if (success)
                                        {
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await _logHelper.LogError("CreatehDashboardExcelperPNR : PNR :- " + noActionPNR.PNRCode + " :-msg:- " + ex.Message);
                                    }
                                    attempts++;
                                }
                                double totalHours = (Disrupted_Flight.STD.Value - TimeZoneService.convertToIND(noActionPNR.CreatedOn.Value)).TotalHours;
                                int hours = (int)Math.Floor(totalHours);
                                double fractionalPart = totalHours - hours;
                                double minutes = Math.Round(fractionalPart * 60, 2);

                                var Current_FlightNo = string.Join(",", journey?.Segments?.Select(x => x.Identifier?.Identifier?.ToString() ?? string.Empty) ?? Enumerable.Empty<string>());
                                NoPNRAction.Cells[$"A{k}"].Value = noActionPNR.PNRCode;
                                NoPNRAction.Cells[$"B{k}"].Value = disruptedFlightNumbers;
                                NoPNRAction.Cells[$"C{k}"].Value = Disrupted_Flight.STD != null ? (Disrupted_Flight.STD ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;
                                NoPNRAction.Cells[$"D{k}"].Value = Current_FlightNo;
                                NoPNRAction.Cells[$"E{k}"].Value = (journey?.Segments != null && journey.Segments.Count > 0 && journey.Segments[0].Designator?.Departure != null) ? journey?.Segments?[0].Designator?.Departure.Value.ToString("dd-MM-yyyy HH:mm:ss") : null;
                                NoPNRAction.Cells[$"F{k}"].Value = CompareFlightNumbers(disruptedFlightNumbers, Current_FlightNo);
                                NoPNRAction.Cells[$"G{k}"].Value = amount < 0 ? "YES" : "NO";
                                NoPNRAction.Cells[$"H{k}"].Value = amount < 0 ? amount : " ";
                                NoPNRAction.Cells[$"I{k}"].Value = PNR_Amount;
                                NoPNRAction.Cells[$"J{k}"].Value = (Disrupted_Flight?.STD.HasValue == true && noActionPNR?.CreatedOn.HasValue == true)
                                    ? $"{hours}.{minutes:00} hr" : (double?)null;
                                NoPNRAction.Cells[$"K{k}"].Value = noActionPNR?.CreatedOn != null ? TimeZoneService.convertToIND(noActionPNR?.CreatedOn.Value ?? new DateTime()).ToString("dd-MM-yyyy HH:mm:ss") : null;

                            }
                            catch
                            {
                                continue;
                            }
                         }
                    }
                    if (!(string.IsNullOrEmpty(filePath)))
                    {
                        await _logHelper.LogInfo("1.Start CreatehDashboardExcel : " + filePath);
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
                await _logHelper.LogError($"{"CreatehDashboardExcel"}{" msg:-"} {ex.Message}:- {"End "}");
                return "";
            }
        }
        private async Task<decimal> CheckNegativeAmounts(BookingDetails? booking)
        {
            try
            {
                if(booking !=null && booking.Data!=null && booking.Data.Payments!=null && booking.Data.Payments.Count()>0)
                {
                    decimal amount = booking.Data.Payments.Where(payment => payment.Amounts?.Amount < 0).Select(payment => payment.Amounts?.Amount).FirstOrDefault()??0;
                    return amount;
                }
                return 0;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{_caller}{"CheckNegativeAmounts"}{" msg:-"} {ex.Message}:- {"End "}");
                return 0;   
            }
        }
        private bool CompareFlightNumbers(string alternateFlightNumbers, string currentFlightNumbers)
        {
            try
            {
                var alternateNumbers = alternateFlightNumbers.Split(',').Select(num => num.Trim().Split('-').Last()).ToList();
                var currentNumbers = currentFlightNumbers.Split(',').Select(num => num.Trim().Split('-').Last()).ToList();
                return !alternateNumbers.Intersect(currentNumbers).Any();
            }
            catch (Exception ex) {
                _logHelper.LogError($"{_caller}{"CheckNegativeAmounts"}{" msg:-"} {ex.Message}:- {"End "}");
                return false;
            }
        }
    }
}
