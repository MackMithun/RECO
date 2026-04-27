using AutoMapper;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class MultipleJourneyService : IMultipleJourneyService
    {
        private readonly ILogHelper _logHelper;
        private readonly string _caller = typeof(ReaccommodationHandlerService).Name;
        private readonly ICheckMoveAvailabilityService _checkMoveAvailabilityService;
        private readonly IMoveAvailabilityServiceForNextDays _moveAvailabilityServiceForNextDays;    
        private readonly IDataMsService _dataMsService;
        private readonly IUpdateJourney _updateJourney;
        private readonly IBookingService _bookingService;
        private readonly IBookingCommit _bookingCommit;
        private readonly IQueueService _queueService;
        private readonly IJourneyService _journeyService;
        private readonly IMapper _mapper;
        public MultipleJourneyService(ILogHelper logHelper, ICheckMoveAvailabilityService checkMoveAvailabilityService, IJourneyService journeyService, IUpdateJourney updateJourney, IBookingService bookingService
            , IMapper mapper, IBookingCommit bookingCommit, IQueueService queueService, IDataMsService dataMsService, IMoveAvailabilityServiceForNextDays moveAvailabilityServiceForNextDays)
        {
            _logHelper = logHelper;
            _checkMoveAvailabilityService = checkMoveAvailabilityService;
            _dataMsService = dataMsService;
            _updateJourney = updateJourney;
            _bookingService = bookingService;
            _bookingCommit = bookingCommit;
            _queueService = queueService;
            _mapper = mapper;
            _journeyService = journeyService;
            _moveAvailabilityServiceForNextDays= moveAvailabilityServiceForNextDays;    
        }
        public async Task<PNRDetails> SuitableflightforMultiplejourneys(Reaccommodation_Model? reaccommodationMs, string Token)
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
                await _logHelper.LogInfo($"{_caller}:{"SuitableflightforMultiplejourneys"} :- {"Start"}");
                if (reaccommodationMs?.bookingDetails?.Data?.Journeys?.Count() > 0)
                {
                    var Tempbooking = reaccommodationMs.bookingDetails?.Data?.Journeys?.Where(journey => journey.Segments.Any(segment => segment?.Identifier?.Identifier == reaccommodationMs.disruptedFlightDB?.FlightNumber));
                    if (Tempbooking != null)
                    {
                        reaccommodationMs.impactedFlight = await _journeyService.SetMCTJourney(reaccommodationMs.bookingDetails, Tempbooking);
                    }
                    MCT_ImpactedJourney impactedJourney = await _journeyService.CheckImpactedJourney(reaccommodationMs);
                    reaccommodationMs?.bookingDetails?.Data?.Journeys?.RemoveAll(Journey => Journey.Segments.All(segment => segment?.Identifier?.Identifier != reaccommodationMs?.disruptedFlightDB?.FlightNumber));
                    impactedJourney = await _journeyService.CheckImpactedJourneyMCT(reaccommodationMs, impactedJourney);
                    if (impactedJourney.ImpactedStatus)
                    {
                        if (!await _journeyService.checkBoardedStatus(reaccommodationMs?.bookingDetails))
                        {
                            if (!(await _journeyService.CheckStretchPNR(reaccommodationMs?.bookingDetails)))
                            {
                                if (!(await _journeyService.CheckCodeShareFlight(reaccommodationMs?.bookingDetails)))
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
                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotImpacted.ToString());
                    }
                }
                else
                {
                    pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                }
                await _logHelper.LogInfo($"{_caller}:{"SuitableflightforMultiplejourneys"} :- {"End"}");
                return pNRandAlternateFlightList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"SuitableflightforMultiplejourneys PNR " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
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
                reaccommodationMs.ToJourneyDetail = await _checkMoveAvailabilityService.ToJourneyKey(reaccommodationMs, Token);
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
                            if(reaccommodationMs.impactedFlight.DepartureStationCode !=null)
                            {
                                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.MCTNotAllowed.ToString());
                            }
                            else
                            {
                                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NoFlightAvailable.ToString());
                            }
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
                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.JourneyKeyMissing.ToString());
                return pNRandAlternateFlightList;
            }

        }

    }
}
