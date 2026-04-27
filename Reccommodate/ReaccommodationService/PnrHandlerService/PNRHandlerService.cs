using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.DatabaseModel;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.LegModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.ManifestModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{

    public class PNRHandlerService : IPNRHandlerService
    {
        private readonly INavitaireTripInfoStatusService _manifestStatusService;
        private readonly INavitaireManifestLegDetailsService _manifestLegDetailsService;
        private readonly IBookingService _bookingService;
        private readonly IDataMsService _dataMsService;
        private readonly INavitaireManifestDetailsService _manifestDetailsService;
        private readonly ILogHelper _logHelper;
        public PNRHandlerService(INavitaireTripInfoStatusService manifestStatusService, INavitaireManifestLegDetailsService manifestLegDetailsService, ILogHelper logHelper,
            IBookingService bookingService, IDataMsService dataMsService, INavitaireManifestDetailsService manifestDetailsService)
        {
            _manifestStatusService = manifestStatusService;
            _manifestLegDetailsService = manifestLegDetailsService;
            _bookingService = bookingService;
            _dataMsService = dataMsService;
            _manifestDetailsService = manifestDetailsService;
            _logHelper = logHelper;
        }
        public async Task<ManifestDetails> GetManifestDetails(DisruptedFlightRequest disruptedFlightRequestModel, string Token)
        {
            try
            {
                ManifestDetails manifestDetailsResponseModel = new ManifestDetails();
                manifestDetailsResponseModel = await _manifestDetailsService.GetManifestDetailsAsync(disruptedFlightRequestModel, Token);
                if (manifestDetailsResponseModel != null && manifestDetailsResponseModel.Data != null && manifestDetailsResponseModel.Data.Count > 0)
                {
                    return manifestDetailsResponseModel;
                }
                else
                {
                    throw new InvalidOperationException("PNRHandlerService -->GetManifestDetails() Mainfest API to contain the Flight Details. please check the Flight Details.");
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetManifestDetails"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<ShortPNRModel?> GetSortedPNR(Reaccommodation_Model reaccommodationMs, string Token)
        {
            try
            {
                ShortPNRModel? shortPNRModel = null;
                int retryCount = 0;
                int maxRetries = 3;

                while (shortPNRModel == null && retryCount < maxRetries)
                {
                    try
                    {
                        shortPNRModel = await fnSortedPNR(reaccommodationMs, Token);
                        retryCount++; 
                    }
                    catch (Exception ex)
                    {
                        await _logHelper.LogError($"Attempt {retryCount} failed: {ex.Message}");
                        retryCount++;
                    }
                }
                if (shortPNRModel != null)
                {
                    return shortPNRModel;
                }
                throw new InvalidOperationException("Not Get the PNR Details : ");
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetSortedPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<ShortPNRModel?> fnSortedPNR(Reaccommodation_Model reaccommodationMs, string Token)
        {
            try
            {
                List<SortedPNR> _listOfPNR = new List<SortedPNR>();
                TripinfoLegListModel? tripinfoLegListModel = await _manifestStatusService.GetLegKeyManifestDetailsAsync(reaccommodationMs.TripInfoLegs, Token);
                if(tripinfoLegListModel !=null)
                {
                    ShortPNRModel? shortPNRModel = await _manifestStatusService.GetLegKeyManifestAsync(tripinfoLegListModel);
                    //**** Get the ParameterList To Database 
                    Dictionary<string, string> appParameterTable = await _dataMsService.GetAppParameterList();
                    List<Task> tasks = new List<Task>();
                    for (int i = 0; i < shortPNRModel?.finalLegKey?.Count; i++)
                    {
                        int index = i;
                        tasks.Add(Task.Run(async () =>
                        {
                            List<SortedPNR> result = await fnSortedDisruptedFlightPNR(reaccommodationMs.disruptedFlightDB, appParameterTable, shortPNRModel?.finalLegKey?[i], reaccommodationMs?.TripInfoLegs?.Identifier, Token);
                            _listOfPNR?.AddRange(result);
                        }));
                        Thread.Sleep(1000);
                    }
                    await Task.WhenAll(tasks);
                    shortPNRModel?._listOfPNR?.AddRange(_listOfPNR.Where(x => !string.IsNullOrEmpty(x.PNRCode)).GroupBy(x => x.PNRCode).Select(x => x.First()));
                    return shortPNRModel;
                }
                return null;    
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- fnSortedPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                return null;
            }
        }
        public async Task<ShortPNRModel?> GetUnSortedPNR(Reaccommodation_Model reaccommodationMs, string Token)
        {
            try
            {
                ShortPNRModel? shortPNRModel = null;
                int retryCount = 0;
                int maxRetries = 3;

                while (shortPNRModel == null && retryCount < maxRetries)
                {
                    try
                    {
                        shortPNRModel = await fnUnSortedPNR(reaccommodationMs, Token);
                        retryCount++;
                    }
                    catch (Exception ex)
                    {
                        await _logHelper.LogError($"Attempt {retryCount} failed: {ex.Message}");
                        retryCount++;
                    }
                }
                if (shortPNRModel != null)
                {
                    return shortPNRModel;
                }
                throw new InvalidOperationException("Not Get the PNR Details : ");
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- GetUnSortedPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task<ShortPNRModel?> fnUnSortedPNR(Reaccommodation_Model reaccommodationMs, string Token)
        {
            try
            {
                List<SortedPNR> _listOfPNR = new List<SortedPNR>();
                TripinfoLegListModel? tripinfoLegListModel = await _manifestStatusService.GetLegKeyManifestDetailsAsync(reaccommodationMs.TripInfoLegs, Token);
                ShortPNRModel? shortPNRModel = await _manifestStatusService.GetLegKeyManifestAsync(tripinfoLegListModel);
                Dictionary<string, string> appParameterTable = await _dataMsService.GetAppParameterList();
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < shortPNRModel?.finalLegKey?.Count; i++)
                {
                    int index = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        List<SortedPNR> result = await fnUNSortedDisruptedFlightPNR(reaccommodationMs.disruptedFlightDB, appParameterTable, shortPNRModel?.finalLegKey?[i], Token);
                        _listOfPNR.AddRange(result);
                    }));
                    Thread.Sleep(1000);
                }
                await Task.WhenAll(tasks);
                shortPNRModel?._listOfPNR?.AddRange(_listOfPNR.Where(x => !string.IsNullOrEmpty(x.PNRCode)).GroupBy(x => x.PNRCode).Select(x => x.First()).ToList());
                return shortPNRModel;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- fnUnSortedPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                return null;
            }
        }
        private async Task<List<SortedPNR>> fnSortedDisruptedFlightPNR(DisruptedFlightDB? disruptedFlightsModelResponse, Dictionary<string, string> appParameterTable, string _LegKey, string? Identifier, string Token)
        {
            try
            {

                List<SortedPNR> _listOfPNR = new List<SortedPNR>();
                LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel = await _manifestStatusService.GetLegKeyStatusAsync(appParameterTable, _LegKey, Token);
                if (legKeyStatusFlightInformationModel != null)
                {
                    if (legKeyStatusFlightInformationModel.status == Enum_DisruptionType.cancelled.ToString() || legKeyStatusFlightInformationModel.status== Enum_DisruptionType.delayed.ToString() || legKeyStatusFlightInformationModel.status == Enum_DisruptionType.advanced.ToString())
                    {
                       _listOfPNR = await _manifestLegDetailsService.GetListShortedPNRAsync(disruptedFlightsModelResponse, _bookingService, _dataMsService, _LegKey, Token);
                    }
                }
                return _listOfPNR;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- fnSortedDisruptedFlightPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
        private async Task<List<SortedPNR>> fnUNSortedDisruptedFlightPNR(DisruptedFlightDB? disruptedFlightsModelResponse, Dictionary<string, string> appParameterTable, string _LegKey, string Token)
        {
            try
            {
                List<SortedPNR> _listOfPNR = new List<SortedPNR>();
                LegKeyStatusFlightInformationModel legKeyStatusFlightInformationModel = await _manifestStatusService.GetLegKeyStatusAsync(appParameterTable, _LegKey, Token);
                if (legKeyStatusFlightInformationModel != null)
                {
                    if (legKeyStatusFlightInformationModel.status == Enum_DisruptionType.cancelled.ToString() || legKeyStatusFlightInformationModel.status == Enum_DisruptionType.delayed.ToString() || legKeyStatusFlightInformationModel.status == Enum_DisruptionType.advanced.ToString())
                    {
                        _listOfPNR = await _manifestLegDetailsService.GetListUnShortedPNRAsync(disruptedFlightsModelResponse, _bookingService,_LegKey, Token);
                    }
                }
                return _listOfPNR;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- fnUNSortedDisruptedFlightPNR"}{" msg:-"} {ex.Message}:- {"End "}");
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}
