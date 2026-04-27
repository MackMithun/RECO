using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class SpecificFlightJourneyService : ISpecificFlightJourneyService
    {
        private readonly ILogHelper _logHelper;
        private readonly IBookingService _bookingService;
        private readonly IUpdateJourney _updateJourney;
        private readonly IBookingCommit _bookingCommit;
        private readonly IQueueService _queueService;
        private readonly IDataMsService _dataMsService;
        private readonly IJourneyService _journeyService;
        private readonly string _caller = typeof(SpecificFlightJourneyService).Name;
        public SpecificFlightJourneyService(ILogHelper logHelper, IBookingService bookingService, IUpdateJourney updateJourney, IBookingCommit bookingCommit,
            IQueueService queueService, IDataMsService dataMsService, IJourneyService journeyService)
        {
            _logHelper = logHelper;
            _bookingService = bookingService;
            _updateJourney = updateJourney;
            _bookingCommit = bookingCommit;
            _queueService = queueService;
            _dataMsService = dataMsService;
            _journeyService = journeyService;
        }

        public async Task<PNRDetails> SpecificflightforSingleJourneys(Reaccommodation_Model? reaccommodationMs, string Token)
        {
            await _logHelper.LogInfo($"{_caller}:{"SpecificflightforSingleJourneys"} :- {"Start"}");
            bool _ProcessStart = true;
            PNRDetails pNRandAlternateFlightList = new PNRDetails();
            pNRandAlternateFlightList.pnrDetail = new List<PNRDetail>();
            pNRandAlternateFlightList.routingDetail = new List<RoutingDetail>();
            try
            {
                reaccommodationMs.bookingService = _bookingService;
                reaccommodationMs.bookingCommit = _bookingCommit;
                reaccommodationMs.queueService = _queueService;
                reaccommodationMs?.bookingDetails?.Data?.Journeys?.RemoveAll(Journey => Journey.Segments.All(segment => segment?.Identifier?.Identifier != reaccommodationMs.disruptedFlightDB?.FlightNumber));
                if (reaccommodationMs !=null && reaccommodationMs?.bookingDetails?.Data?.Journeys?.Count() > 0)
                {
                    if(!await _journeyService.checkBoardedStatus(reaccommodationMs?.bookingDetails))
                    {
                        if (!(await _journeyService.CheckStretchPNR(reaccommodationMs?.bookingDetails)))
                        {

                            if (!(await _journeyService.CheckCodeShareFlight(reaccommodationMs?.bookingDetails)))
                            {
                                if (await _journeyService.checkMultiSegments(reaccommodationMs))
                                {
                                    string SSRCode = await _journeyService.checkSSR(reaccommodationMs);
                                    if (string.IsNullOrEmpty(SSRCode))
                                    {
                                        await _logHelper.LogInfo($"{_caller}:{"SpecificflightforSingleJourneys"} :- {"PNR :-"}{reaccommodationMs?.PNRDetail?.PNRCode ?? ""}{"SegmentKey :-"}{reaccommodationMs?.ManifestDetail?.Data?[0].Journeys?[0].Segments?[0].SegmentKey ?? ""}");
                                        reaccommodationMs.ToJourneyDetail = new ToJourneyDetails();
                                        reaccommodationMs.ToJourneyDetail.JourneyKey = reaccommodationMs.ManifestDetail?.Data?[0].Journeys?[0].Segments?[0].SegmentKey;
                                        if(!await _journeyService.HandleUndoCheckingTask(reaccommodationMs?.bookingDetails, Token))
                                        {
                                            pNRandAlternateFlightList = await _updateJourney.SetUpdateJourney(reaccommodationMs, Token);
                                        }
                                        else
                                        {
                                            pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs.PNRDetail, false, Enum_PNR.JourneyCheckedIn.ToString());
                                        }
                                    }
                                    else
                                    {
                                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs.PNRDetail, false, Enum_PNR.restrictedSSR.ToString() + " " + SSRCode);
                                    }
                                }
                                else
                                {
                                    pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs.PNRDetail, false, Enum_PNR.MultiSegments.ToString());
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
                    pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(null, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                }
                return pNRandAlternateFlightList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"SpecificflightforSingleJourneys PNR :" + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {ex.Message}");
                PNRDetails pNRandAlternate = new PNRDetails();
                pNRandAlternate.pnrDetail = new List<PNRDetail>();
                pNRandAlternate.routingDetail = new List<RoutingDetail>();
                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(null, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                pNRandAlternateFlightList.pnrDetail.AddRange(pNRandAlternate.pnrDetail);
                pNRandAlternateFlightList.routingDetail.AddRange(pNRandAlternate.routingDetail);

                return pNRandAlternateFlightList;
            }
        }

        public async Task<PNRDetails> SpecificflightforMultiplejourneys(Reaccommodation_Model? reaccommodationMs, string Token)
        {
            await _logHelper.LogInfo($"{_caller}:{"SpecificflightforMultiplejourneys"} :- {"Start"}");
            PNRDetails pNRandAlternateFlightList = new PNRDetails();
            pNRandAlternateFlightList.pnrDetail = new List<PNRDetail>();
            pNRandAlternateFlightList.routingDetail = new List<RoutingDetail>();
            try
            {
                reaccommodationMs.bookingService = _bookingService;
                reaccommodationMs.bookingCommit = _bookingCommit;
                reaccommodationMs.queueService = _queueService;
                await _logHelper.LogInfo($"{_caller}:{"SpecificflightforMultiplejourneys"} :- {"PNR "}" + reaccommodationMs?.PNRDetail?.PNRCode ?? "MCTImpactedFlight" + reaccommodationMs?.impactedFlight.mctApplicable);
                if (reaccommodationMs != null && reaccommodationMs.bookingDetails?.Data != null && reaccommodationMs.bookingDetails.Data.Journeys != null && reaccommodationMs.bookingDetails.Data.Journeys.Any())
                {
                    var Tempbooking = reaccommodationMs?.bookingDetails?.Data?.Journeys.Where(journey => journey.Segments.Any(segment => segment?.Identifier?.Identifier == reaccommodationMs?.disruptedFlightDB?.FlightNumber));
                    if (Tempbooking != null)
                    {
                        reaccommodationMs.impactedFlight = await _journeyService.SetMCTJourney(reaccommodationMs.bookingDetails, Tempbooking);
                    }
                    if (reaccommodationMs.impactedFlight.mctApplicable)
                    {
                        reaccommodationMs.impactedFlight = await _dataMsService.GetStationConnectionRuleList(reaccommodationMs.impactedFlight);
                    }
                    if (!reaccommodationMs.impactedFlight.mctApplicable || reaccommodationMs.ManifestDetail?.Data?[0].Journeys?[0].Designator?.Departure > reaccommodationMs.impactedFlight.NearestJourneySAT && reaccommodationMs.ManifestDetail?.Data?[0].Journeys?[0].Designator?.Arrival < reaccommodationMs.impactedFlight.NearestJourneySDT)
                    {
                        reaccommodationMs.bookingDetails?.Data?.Journeys?.RemoveAll(Journey => Journey.Segments.All(segment => segment?.Identifier?.Identifier != reaccommodationMs?.disruptedFlightDB?.FlightNumber));
                        if (!await _journeyService.checkBoardedStatus(reaccommodationMs?.bookingDetails))
                        {
                            if (await _journeyService.checkMultiSegments(reaccommodationMs))
                            {
                                if (!(await _journeyService.CheckStretchPNR(reaccommodationMs?.bookingDetails)))
                                {
                                    if (!(await _journeyService.CheckCodeShareFlight(reaccommodationMs?.bookingDetails)))
                                    {
                                        string SSRCode = await _journeyService.checkSSR(reaccommodationMs);
                                        if (string.IsNullOrEmpty(SSRCode))
                                        {
                                            await _logHelper.LogInfo($"{_caller}:{"SpecificflightforMultiplejourneys"} :- {"PNR :-"}{reaccommodationMs?.PNRDetail?.PNRCode ?? ""}{"SegmentKey :-"}{reaccommodationMs?.ManifestDetail?.Data?[0].Journeys?[0].Segments?[0].SegmentKey ?? ""}");
                                            reaccommodationMs.ToJourneyDetail = new ToJourneyDetails();
                                            reaccommodationMs.ToJourneyDetail.JourneyKey = reaccommodationMs.ManifestDetail?.Data?[0].Journeys?[0].Segments?[0].SegmentKey;
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
                                            pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs.PNRDetail, false, Enum_PNR.restrictedSSR.ToString() + " " + SSRCode);
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
                                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs.PNRDetail, false, Enum_PNR.MultiSegments.ToString());
                            }
                        }
                        else
                        {
                            pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs.PNRDetail, false, Enum_PNR.JourneyBoarded.ToString());
                        }
                    }
                    else
                    {
                        reaccommodationMs.bookingDetails?.Data?.Journeys?.RemoveAll(Journey => Journey.Segments.All(segment => segment?.Identifier?.Identifier != reaccommodationMs?.disruptedFlightDB?.FlightNumber));
                        pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.MCTNotAllowed.ToString());

                    }
                }
                else
                {
                    pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(reaccommodationMs?.bookingDetails, null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                }


                return pNRandAlternateFlightList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{"SpecificflightforMultiplejourneys PNR :" + reaccommodationMs?.PNRDetail?.PNRCode} :- {ex.Message}");
                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(null, null, null, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                return pNRandAlternateFlightList;
            }
        }
    }
}
