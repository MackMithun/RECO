using AutoMapper;
using Newtonsoft.Json;
using OfficeOpenXml;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using System.Globalization;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class DashboardDetailsService : IDashboardDetailsService
    {
        private readonly INavitaireAuthorizationService _authorizationService;
        private readonly IDataMsService _dataMsService;
        private readonly IDashboardDetailsPerPNR _dashboardDetailsPerPNR;
        private readonly ILogHelper _logHelper;
        private readonly IMapper _mapper;
        private readonly string _caller = typeof(DashboardDetailsService).Name;
        public DashboardDetailsService(ILogHelper logHelper, IMapper mapper, INavitaireAuthorizationService navitaireAuthorizationService, IDataMsService dataMsService, IDashboardDetailsPerPNR dashboardDetailsPerPNR)
        {
            _logHelper = logHelper; 
            _mapper = mapper;
            _authorizationService = navitaireAuthorizationService;  
            _dataMsService = dataMsService;
            _dashboardDetailsPerPNR = dashboardDetailsPerPNR;
        }
        public  async Task fnGenrateDashboardDetails(DashboardDetails disruptedFlight)
        {
            try
            {
                bool _status=false;
                string _token = await _authorizationService.GetTokenAsync();
                if(!string.IsNullOrEmpty(_token))
                {
                    await _logHelper.LogInfo($"{_caller}{" fnGenrateDashboardDetails "}{" StartTime - EndTime : "}{disruptedFlight.startDate}{"-"}{disruptedFlight.endDate}");
                    FetchDashboardRequest fetchDashboard = _mapper.Map<FetchDashboardRequest>(disruptedFlight);
                    Dictionary<string, string> AppParameterList =await _dataMsService.GetAppParameterList();
                    FetchDashboardResponse fetchDashboardResponses = await _dataMsService.fetchDashboardDetails(fetchDashboard);
                    NhbTemplateResponse nhbTemplate = await _dataMsService.GetNhbTemplate();
                    _status = await fnGenrateDashboardForPNR(disruptedFlight,fetchDashboardResponses, AppParameterList, nhbTemplate, _token);
                }
                else
                {
                    throw new InvalidOperationException("Token Not Genrated.");
                }
                await _logHelper.LogInfo($"{_caller}{" fnGenrateDashboardDetails :"}{" _status : "}{_status}");
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{_caller}{"fnGenrateDashboardDetails"}{" msg:-"} {ex.Message}:- {"End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<DashboardResponse> fnDashboardDetails()
        {
            try
            {
                //string filePath = @"Configs\DashboardDetails.xlsx";
                string filePath = @"/app/Configs/DashboardDetails.xlsx";  //OCP Server
                DashboardResponse dashboardResponse = new DashboardResponse();
                FileInfo fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists)
                    throw new FileNotFoundException("Excel file not found at the specified path.");

                using (var package = new ExcelPackage(fileInfo))
                {
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        switch (worksheet.Name)
                        {
                            case "PNR Reaccommodated":
                                dashboardResponse.PnrReaccommodated = ReadPNRWorksheet(worksheet);
                                break;
                            case "PNR Not Reaccommodated":
                                dashboardResponse.PnrNotReaccommodated = ReadPNRWorksheet(worksheet);
                                break;
                            case "No PNR Action":
                                dashboardResponse.PnrNoAction = ReadPNRWorksheet(worksheet);
                                break;
                            default:
                                break; // Skip other worksheets
                        }
                    }
                }
                return dashboardResponse;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{_caller}{"fnDashboardDetails"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new FileNotFoundException(ex.Message);
            }
        }
        private async Task<bool> fnGenrateDashboardForPNR(DashboardDetails disruptedFlight,FetchDashboardResponse fetchDashboardResponse, Dictionary<string, string> AppParameterList, NhbTemplateResponse nhbTemplate, string _token)
        {
            try
            {
                bool _status = false;
                _status = await _dashboardDetailsPerPNR.fnGenrateDashboardPerPNR(disruptedFlight,fetchDashboardResponse, AppParameterList, nhbTemplate, _token);
                return _status;
            }
            catch(Exception ex)
            {
                await _logHelper.LogError($"{_caller}{"fnGenrateDashboardForPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                return false;
            }
        }
        private PnrReport ReadPNRWorksheet(ExcelWorksheet worksheet)
        {
            PnrReport pnrReport = new PnrReport { PNRDetailReportItem = new List<PNRDashDetail>() };
            int startRow = 3; // Assuming data starts from row 3
            int totalRows = worksheet.Dimension.Rows;
            int totalCols = worksheet.Dimension.Columns;

            for (int row = startRow; row <= totalRows; row++)
            {
                PNRDashDetail detail = new PNRDashDetail
                {
                    PNRCode = worksheet.Cells[row, 1].Text,
                    DisruptedFlightNo = worksheet.Cells[row, 2].Text,
                    DisruptedFlightDate = ParseDate(worksheet.Cells[row, 3].Text),
                    RecoFlightNo = worksheet.Cells[row, 4].Text,
                    RecoFlightDate = ParseDate(worksheet.Cells[row, 5].Text),
                    CurrentFlightNo = worksheet.Cells[row, 6].Text,
                    CurrentFlightDate = ParseDate(worksheet.Cells[row, 7].Text),
                    FlightChangeStatus = worksheet.Cells[row, 8].Text,
                    RefundStatus = worksheet.Cells[row, 9].Text,
                    RefundAmount = ParseAmount(worksheet.Cells[row, 10].Text)
                };

                pnrReport.PNRDetailReportItem.Add(detail);
            }
            return pnrReport;
        }

        private DateTime? ParseDate(string dateValue)
        {
            if (DateTime.TryParseExact(dateValue, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }
            return null;
        }

        private int ParseAmount(string amountValue)
        {
            if (int.TryParse(amountValue, out int amount))
            {
                return amount;
            }
            return 0;
        }
    }
}
