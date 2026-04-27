using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.RequestModel;
using AutoMapper;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class SingleJourneyService : ISingleJourneyService
    {
        private readonly ILogHelper _logHelper;
        private readonly ICheckMoveAvailabilityService _checkMoveAvailabilityService;
        private readonly IMoveAvailabilityServiceForNextDays _moveAvailabilityServiceForNextDays;
        private readonly IMapper _mapper;
        private readonly string _caller = typeof(SingleJourneyService).Name;
        private readonly IJourneyService _journeyService;
        private readonly IUpdateJourney _updateJourney;
        private readonly IBookingService _bookingService;
        private readonly IBookingCommit _bookingCommit;
        private readonly IQueueService _queueService;
        private readonly IDataMsService _dataMsService;
        public SingleJourneyService(ILogHelper logHelper, ICheckMoveAvailabilityService checkMoveAvailabilityService, IMoveAvailabilityServiceForNextDays moveAvailabilityServiceForNextDays, IUpdateJourney updateJourney, IBookingService bookingService, IBookingCommit bookingCommit,
          IQueueService queueService, IMapper mapper, IJourneyService journeyService, IDataMsService dataMsService)
        {
            _logHelper = logHelper;
            _checkMoveAvailabilityService = checkMoveAvailabilityService;
            _moveAvailabilityServiceForNextDays = moveAvailabilityServiceForNextDays;
            _journeyService = journeyService;
            _updateJourney = updateJourney;
            _bookingService = bookingService;
            _bookingCommit = bookingCommit;
            _queueService = queueService;
            _mapper = mapper;
            _dataMsService = dataMsService;
        }
        public async Task<PNRDetails> SuitableflightforSingleJourneys(Reaccommodation_Model? reaccommodationMs, string Token)
        {
            await _logHelper.LogInfo($"{_caller}:{"SuitableflightforSinglejourneys"} :- {"Start"}");
            PNRDetails pNRandAlternateFlightList = new PNRDetails();
            pNRandAlternateFlightList.pnrDetail = new List<PNRDetail>();
            pNRandAlternateFlightList.routingDetail = new List<RoutingDetail>();
            try
            {
                reaccommodationMs?.bookingDetails?.Data?.Journeys?.RemoveAll(Journey => Journey.Segments.All(segment => segment?.Identifier?.Identifier != reaccommodationMs?.disruptedFlightDB?.FlightNumber));
                if (reaccommodationMs?.bookingDetails?.Data?.Journeys?.Count() > 0)
                {
                    if (!await _journeyService.checkBoardedStatus(reaccommodationMs?.bookingDetails))
                    {
                        if (!(await _journeyService.CheckStretchPNR(reaccommodationMs?.bookingDetails)))
                        {
                            if (!(await _journeyService.CheckCodeShareFlight(reaccommodationMs?.bookingDetails)))
                            {
                                if (await CheckImpactedJourney(reaccommodationMs))
                                {
                                    string ssrCode = await _journeyService.checkSSR(reaccommodationMs);
                                    if (string.IsNullOrEmpty(ssrCode))
                                    {
                                        if (reaccommodationMs?.bookingDetails?.Data?.Passengers != null && reaccommodationMs?.bookingDetails?.Data?.Passengers.Count() > 0)
                                        {
                                            pNRandAlternateFlightList = await MapPNRandAlternateFlightList(reaccommodationMs, Token);
                                        }
                                        else
                                        {
                                            pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NoPassengerAvailability.ToString());
                                        }
                                    }
                                    else
                                    {
                                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.restrictedSSR.ToString() + " " + ssrCode);
                                    }
                                }
                                else
                                {
                                    pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotImpacted.ToString());
                                }
                            }
                            else
                            {
                                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.CodeShare.ToString());
                            }
                        }
                        else
                        {
                            pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.StretchPNR.ToString());
                        }
                    }
                    else
                    {
                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.JourneyBoarded.ToString());
                    }
                }
                else
                {
                    pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                }
                await _logHelper.LogInfo($"{_caller}:{"SuitableflightforSinglejourneys"} :- {"End"}");
                return pNRandAlternateFlightList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"SuitableflightforSinglejourneys"} :- {ex.Message}");
                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(null, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                return pNRandAlternateFlightList;
            }
        }
        private async Task<PNRDetails> MapPNRandAlternateFlightList(Reaccommodation_Model? reaccommodationMs, string Token)
        {

            PNRDetails pNRandAlternateFlightList = new PNRDetails();
            pNRandAlternateFlightList.pnrDetail = new List<PNRDetail>();
            pNRandAlternateFlightList.routingDetail = new List<RoutingDetail>();
            try
            {
                reaccommodationMs.dataMsService = _dataMsService;
                reaccommodationMs.bookingService = _bookingService;
                reaccommodationMs.bookingCommit = _bookingCommit;
                reaccommodationMs.queueService = _queueService;
                reaccommodationMs.ToJourneyDetail = await _checkMoveAvailabilityService.ToJourneyKey(reaccommodationMs, Token);
                if (reaccommodationMs.ToJourneyDetail != null && reaccommodationMs.ToJourneyDetail.JourneyKey != "")
                {
                    if (!await _journeyService.HandleUndoCheckingTask(reaccommodationMs?.bookingDetails, Token))
                    {
                        pNRandAlternateFlightList = await _updateJourney.SetUpdateJourney(reaccommodationMs, Token);
                    }
                    else
                    {
                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.JourneyCheckedIn.ToString());
                    }
                }
                else
                {
                    if (reaccommodationMs?.disruptedFlightDB?.DisruptionType.ToLower().Trim() == Enum_DisruptionType.cancelled.ToString())
                    {
                        await _logHelper.LogInfo($"{_caller}:{"check Next Availability_ToJourneyKey"} :- {" ToJourneyKey Start"}");
                        reaccommodationMs.ToJourneyDetail = await _moveAvailabilityServiceForNextDays.ToJourneyKey(reaccommodationMs, Token);
                        if (reaccommodationMs.ToJourneyDetail != null && !string.IsNullOrEmpty(reaccommodationMs.ToJourneyDetail.JourneyKey))
                        {
                            if (!await _journeyService.HandleUndoCheckingTask(reaccommodationMs?.bookingDetails, Token))
                            {
                                pNRandAlternateFlightList = await _updateJourney.SetUpdateJourney(reaccommodationMs, Token);
                            }
                            else
                            {
                                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.JourneyCheckedIn.ToString());
                            }
                                
                        }
                        else
                        {
                            pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NoFlightAvailable.ToString());
                        }
                    }
                    else
                    {
                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NoFlightAvailable.ToString());
                    }
                }
                return pNRandAlternateFlightList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"MapPNRandAlternateFlightList"} :- {ex.Message}");
                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.JourneyKeyMissing.ToString());
                return pNRandAlternateFlightList;
            }
        }
        private async Task<bool> CheckImpactedJourney(Reaccommodation_Model? reaccommodation_Model)
        {
            MCT_SingleImpactedJourney Single_ImpactedJourney = new MCT_SingleImpactedJourney();
            if (reaccommodation_Model?.disruptedFlightDB?.DisruptionType.ToLower().Trim() == Enum_DisruptionType.delayed.ToString())//_filghtStatus  "Canceled" ||  "Delay" ||Advanced
            {
                // check Currect J : ETA and Next Journey ETD / STDstring? destination = "";
                Single_ImpactedJourney.FlightType = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].FlightType.ToString();
                for (int j = 0; j < reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?.Count; j++)
                {
                    if (reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[j].Identifier?.Identifier == reaccommodation_Model?.disruptedFlightDB?.FlightNumber)
                    {
                        DateTime STA = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Designator.Arrival;
                        if (j + 1 < reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?.Count)
                        {
                            Single_ImpactedJourney.ImpactedNearestJourneyTime = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys[0]?.Segments[j + 1].Designator?.Departure;
                            Single_ImpactedJourney.ImpactedStationCode = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[j + 1].Designator?.Destination;
                            Single_ImpactedJourney.ImpactedNearestJourneyTime.AddMinutes(-60);
                            if (Single_ImpactedJourney.ImpactedNearestJourneyTime > STA)
                            {
                                Single_ImpactedJourney.ImpactedStatus = false;
                            }
                            else
                            {
                                Single_ImpactedJourney.ImpactedStatus = true;
                            }

                        }
                    }
                }
            }
            else if (reaccommodation_Model?.disruptedFlightDB?.DisruptionType.ToLower().Trim() == Enum_DisruptionType.advanced.ToString())
            {
                // Check Currect J : ETD and Previous ETA/STA
                Single_ImpactedJourney.FlightType = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].FlightType.ToString();
                for (int j = 0; j < reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?.Count; j++)
                {
                    if (reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[j].Identifier?.Identifier == reaccommodation_Model?.disruptedFlightDB?.FlightNumber)
                    {
                        DateTime STD = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys[0].Designator.Departure;
                        // Check if there is a next segment in the same journey
                        if (j - 1 >= 0)
                        {
                            Single_ImpactedJourney.ImpactedNearestJourneyTime = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[j - 1].Designator?.Arrival;
                            Single_ImpactedJourney.ImpactedStationCode = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Segments?[j - 1].Designator?.Origin;
                            Single_ImpactedJourney.ImpactedNearestJourneyTime.AddMinutes(60);
                            if (Single_ImpactedJourney.ImpactedNearestJourneyTime < STD)
                            {
                                Single_ImpactedJourney.ImpactedStatus = false;
                            }
                            else
                            {
                                Single_ImpactedJourney.ImpactedStatus = true;
                            }
                        }
                    }
                }

            }
            else
            {
                Single_ImpactedJourney.ImpactedStatus = true;
            }
            return Single_ImpactedJourney.ImpactedStatus;
        }
    }
}
