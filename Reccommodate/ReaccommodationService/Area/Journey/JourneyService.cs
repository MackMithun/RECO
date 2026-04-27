using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.Utilities;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class JourneyService : IJourneyService
    {
        private readonly ILogHelper _logHelper;
        private readonly ICheckingService _checkingService;   
        public JourneyService(ILogHelper logHelper, ICheckingService checkingService)
        {
            _logHelper = logHelper;
            _checkingService = checkingService;     
        }
        public async Task<MCT_ImpactedFlight> SetMCTJourney(BookingDetails? booking, IEnumerable<Models.ResponseModel.BookingModel.Journey>? Tempbooking)
        {
            try
            {
                MCT_ImpactedFlight multi_ImpactedFlightModel = new MCT_ImpactedFlight();
                foreach (var tempJourney in Tempbooking)
                {
                    multi_ImpactedFlightModel.NearestJourneySAT = tempJourney.Segments?[0].Designator?.Arrival;
                    multi_ImpactedFlightModel.NearestJourneySDT = tempJourney.Segments?[0].Designator?.Departure;
                    multi_ImpactedFlightModel.flightType = tempJourney.FlightType;
                    multi_ImpactedFlightModel.ArrivalStationCode = null;
                    multi_ImpactedFlightModel.DepartureStationCode = null;
                }
                foreach (var journey in booking.Data.Journeys)
                {
                    DateTime arrivalTime = (DateTime)journey.Designator.Arrival;
                    DateTime departureTime = (DateTime)journey.Designator.Departure;
                    // Compare departure time
                    //starting NearestJourneySDT is the Real Departure Time
                    if (multi_ImpactedFlightModel.NearestJourneySDT < departureTime)
                    {
                        multi_ImpactedFlightModel.mctApplicable = true;
                        multi_ImpactedFlightModel.NearestJourneySDT = departureTime;
                        multi_ImpactedFlightModel.DepartureStationCode = journey.Designator.Origin;
                    }
                    // Compare arrival time
                    if (multi_ImpactedFlightModel.NearestJourneySDT > departureTime)
                    {
                        multi_ImpactedFlightModel.mctApplicable = true;
                        multi_ImpactedFlightModel.NearestJourneySAT = arrivalTime;
                        multi_ImpactedFlightModel.ArrivalStationCode = journey.Designator.Destination;
                    }
                }
                return multi_ImpactedFlightModel;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"JourneyService"}:{"SetMCTJourney"} :- {"End msg :" + ex.Message}");
                return new  MCT_ImpactedFlight();
            }
            
        }
        public async Task<MCT_ImpactedJourney> CheckImpactedJourney(Reaccommodation_Model? reaccommodation_Model)
        {
            try
            {
                MCT_ImpactedJourney multi_ImpactedJourney = new MCT_ImpactedJourney();
                if (reaccommodation_Model?.disruptedFlightDB?.DisruptionType.ToLower().Trim() == Enum_DisruptionType.delayed.ToString())//_filghtStatus  "Canceled" ||  "Delay" ||Advanced
                {
                    // check Currect J : ETA and Next Journey ETD / STDstring? destination = "";
                    for (int i = 0; i < reaccommodation_Model?.bookingDetails?.Data?.Journeys.Count; i++)
                    {
                        multi_ImpactedJourney.FlightType = reaccommodation_Model?.bookingDetails?.Data?.Journeys[i].FlightType.ToString();
                        for (int j = 0; j < reaccommodation_Model?.bookingDetails?.Data?.Journeys[i]?.Segments?.Count; j++)
                        {
                            if (reaccommodation_Model?.bookingDetails?.Data?.Journeys[i].Segments[j].Identifier?.Identifier == reaccommodation_Model?.disruptedFlightDB?.FlightNumber)
                            {
                                // Check if there is a next segment in the same journey
                                if (j + 1 < reaccommodation_Model?.bookingDetails?.Data?.Journeys[i].Segments.Count)
                                {
                                    multi_ImpactedJourney.ImpactedStatus = true;
                                    multi_ImpactedJourney.ImpactedNearestJourneyTime = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys[i]?.Segments[j + 1].Designator?.Departure;
                                    multi_ImpactedJourney.ImpactedStationCode = reaccommodation_Model?.bookingDetails?.Data?.Journeys[i].Segments[j + 1].Designator?.Destination;
                                }
                                else
                                {
                                    if (i + 1 < reaccommodation_Model?.bookingDetails?.Data?.Journeys?.Count)
                                    {
                                        multi_ImpactedJourney.ImpactedStatus = true;
                                        multi_ImpactedJourney.ImpactedNearestJourneyTime = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys[i + 1]?.Designator?.Departure;
                                        multi_ImpactedJourney.ImpactedStationCode = reaccommodation_Model?.bookingDetails?.Data?.Journeys[i + 1].Designator?.Destination;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (reaccommodation_Model?.disruptedFlightDB?.DisruptionType.ToLower().Trim() == Enum_DisruptionType.advanced.ToString())
                {
                    // Check Currect J : ETD and Previous ETA/STA
                    for (int i = 0; i < reaccommodation_Model?.bookingDetails?.Data?.Journeys?.Count; i++)
                    {
                        multi_ImpactedJourney.FlightType = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[i].FlightType.ToString();
                        for (int j = 0; j < reaccommodation_Model?.bookingDetails?.Data?.Journeys?[i].Segments?.Count; j++)
                        {
                            if (reaccommodation_Model?.bookingDetails?.Data?.Journeys?[i].Segments?[j].Identifier?.Identifier == reaccommodation_Model?.disruptedFlightDB?.FlightNumber)
                            {
                                // Check if there is a next segment in the same journey
                                if (j - 1 >= 0)
                                {
                                    multi_ImpactedJourney.ImpactedStatus = true;
                                    multi_ImpactedJourney.ImpactedNearestJourneyTime = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys[i]?.Segments[j - 1].Designator?.Arrival;
                                    multi_ImpactedJourney.ImpactedStationCode = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[i].Segments?[j - 1].Designator?.Origin;
                                }
                                else
                                {
                                    if (i - 1 >= 0)
                                    {

                                        multi_ImpactedJourney.ImpactedStatus = true;
                                        multi_ImpactedJourney.ImpactedNearestJourneyTime = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys[i - 1]?.Designator?.Arrival;
                                        multi_ImpactedJourney.ImpactedStationCode = reaccommodation_Model?.bookingDetails?.Data?.Journeys?[i - 1].Designator?.Origin;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    multi_ImpactedJourney.ImpactedStatus = true;
                }
                return multi_ImpactedJourney;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"JourneyService"}:{"checkSSR PNR " + reaccommodation_Model?.PNRDetail?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
            }
            return new MCT_ImpactedJourney();


        }
        public async Task<MCT_ImpactedJourney> CheckImpactedJourneyMCT(Reaccommodation_Model? reaccommodation_Model, MCT_ImpactedJourney impactedJourney)
        {
            if (impactedJourney.ImpactedStatus)
            {
                if (reaccommodation_Model?.disruptedFlightDB?.DisruptionType == Enum_DisruptionType.delayed.ToString() || reaccommodation_Model?.disruptedFlightDB?.DisruptionType == Enum_DisruptionType.advanced.ToString())
                {
                    double _minTime = await reaccommodation_Model.dataMsService.CheckMCTConnectionTime(impactedJourney.ImpactedStationCode, impactedJourney.FlightType);
                    DateTime STD = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Designator?.Departure;
                    DateTime STA = (DateTime)reaccommodation_Model?.bookingDetails?.Data?.Journeys?[0].Designator?.Arrival;
                    impactedJourney.ImpactedStatus = await ActionOnImpactedJourney(impactedJourney, reaccommodation_Model?.disruptedFlightDB?.DisruptionType.ToLower().Trim(), STD, STA, _minTime);
                }
            }
            return impactedJourney;
        }
        public async Task<string> checkSSR(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                var passengerSegment = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[0].passengerSegment;
                if (passengerSegment != null && passengerSegment.Count > 1)
                {
                    bool anySsrIsNull = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?.SelectMany(segment => segment.passengerSegment.Values).Any(ps => ps.ssrs == null)??true;
                    if(!anySsrIsNull)
                    {
                        foreach (var passengerS in reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[0].passengerSegment) {
                            string ssrCode= passengerS.Value.ssrs?[0].ssrCode?.ToUpper() ?? "";
                            if (!(reaccommodationMs.ListofRestrictedSSR.Contains(ssrCode)))
                            {
                                return "";
                            }
                        }
                        return passengerSegment?.First().Value?.ssrs?[0].ssrCode?.ToUpper() ?? "";
                    }
                }
                else if (passengerSegment != null && passengerSegment.Count > 0 && passengerSegment?.First().Value?.ssrs?.Count > 0)
                {
                    string ssrCode = passengerSegment?.First().Value?.ssrs?[0].ssrCode?.ToUpper() ?? "";
                    if (reaccommodationMs.ListofRestrictedSSR.Contains(ssrCode))
                    {
                        return ssrCode;
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"JourneyService"}:{"checkSSR PNR " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return "";
            }
        }
        public async Task<bool> ActionOnImpactedJourney(MCT_ImpactedJourney impactedJourney, string? DisruptedFlight, DateTime STD, DateTime STA, double _minTime)
        {
            bool _status = true;
            if (DisruptedFlight == Enum_DisruptionType.delayed.ToString())
            {
                impactedJourney.ImpactedNearestJourneyTime.AddMinutes(-_minTime);
                if (impactedJourney.ImpactedNearestJourneyTime > STA)
                {
                    _status = false;
                }
            }
            else if (DisruptedFlight == Enum_DisruptionType.advanced.ToString())
            {
                impactedJourney.ImpactedNearestJourneyTime.AddMinutes(_minTime);
                if (impactedJourney.ImpactedNearestJourneyTime < STD)
                {
                    _status = false;
                }
            }
            return _status;
        }
        public async Task<bool> checkMultiSegments(Reaccommodation_Model reaccommodationMs)
        {
            try
            {
                if (reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?.Count > 1)
                {
                    int totalSegment = Convert.ToInt32(reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?.Count());
                    string? S1Origin = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Designator?.Origin;
                    string? S1Destination = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[totalSegment - 1].Designator?.Destination;
                    if (!(reaccommodationMs?.DisruptedFlight?.origin?.ToUpper() == S1Origin?.ToUpper() && reaccommodationMs?.DisruptedFlight?.destination?.ToUpper() == S1Destination?.ToUpper()))
                    {
                        return false;
                    }

                }
                else
                {
                    string? S1Origin = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Designator?.Origin;
                    string? S1Destination = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Designator?.Destination;
                    if (!(reaccommodationMs?.DisruptedFlight?.origin?.ToUpper() == S1Origin?.ToUpper() && reaccommodationMs?.DisruptedFlight?.destination?.ToUpper() == S1Destination?.ToUpper()))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"JourneyService"}:{"checkMultiSegments PNR " + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> CheckCodeShareFlight(BookingDetails? booking)
        {
            try
            {
                //= booking.Data.Journeys.Any(x => x.Segments.Any(x => x.Legs.Any(x => x.LegInfo?.CodeShareIndicator != 0)));
                bool _status= booking?.Data?.Locators?.RecordLocators?.Any()??false;
                if(_status)
                {
                    List<string> bookingSystemCode = ConfigurationUtilities.GetValuefromConfig(Enum_CodeShare.recordLocators.ToString(), Enum_CodeShare.bookingSystemCode.ToString()).Split(',').ToList();
                    List<string> interactionPurpose = ConfigurationUtilities.GetValuefromConfig(Enum_CodeShare.recordLocators.ToString(), Enum_CodeShare.interactionPurpose.ToString()).Split(',').ToList();
                    _status = booking?.Data?.Locators?.RecordLocators?.Any(x => bookingSystemCode.Contains(x.BookingSystemCode??"") || interactionPurpose.Contains(x.InteractionPurpose??""))??false;                   
                }
                return _status;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"CheckCodeShareFlight"} :- {ex.Message}");
                return false;       
            }
        }
        public async Task<bool> CheckStretchPNR(BookingDetails? booking)
        {
            try
            {
                bool _Status=false;
                var CheckStretchPNR = booking?.Data?.Journeys?.Any(journey => journey?.Segments?.Any(segment => segment?.Fares?
                .Any(fare => fare?.ProductClass?.ToUpper().Trim() == "BC" || fare?.ProductClass?.ToUpper().Trim() == "BR") ?? false) ?? false) ?? false;
                if(CheckStretchPNR)
                {
                    _Status = true;     
                }
                return _Status; 
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{":- CheckStretchPNR"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> HandleUndoCheckingTask(BookingDetails? booking,string Token)
        {
            try
            {
                bool _Status=false;
                bool liftStatus = booking?.Data?.Journeys?[0].Segments?.Any(x => x.passengerSegment.Values.Any(y => y.liftStatus == 1))??false;
                if(liftStatus)
                {
                    bool bags = booking?.Data?.Passengers?.Values.Any(x=>x.Bags!=null && x.Bags.Count>0)??false;
                    if(bags)
                    {
                        _Status=true;
                    }
                    else
                    {
                        bool _undoChecking = await _checkingService.checkOutJourney(booking, Token);
                        await _logHelper.LogInfo($"checkOutJourney : {"PNR : "}{booking.Data?.RecordLocator??""}{":-"}{_undoChecking}");
                    }
                }
                return _Status;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{":- CheckStretchPNR"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> checkBoardedStatus(BookingDetails? booking)
        {
            try
            {
                bool _Status = false; 
                DateTime? DepartureTimeUtc = booking?.Data?.Journeys?[0].Segments?[0].Legs?[0].LegInfo?.DepartureTimeUtc;
                DateTime? UtcNow = DateTime.UtcNow;
                bool liftStatus = booking?.Data?.Journeys?[0].Segments?.Any(x => x.passengerSegment.Values.Any(y => y.liftStatus == 2 || y.liftStatus == 3)) ?? false;
                if(liftStatus || (UtcNow > DepartureTimeUtc))
                {
                    _Status = true;
                }
                return _Status;
            }
            catch (Exception ex)
            {
                return false;   
            }
        }
        public async Task<PNRDetails> GetAccommodationByFlightNoResponseModel(BookingDetails? oldbooking, BookingDetails? newbooking, PNRDetail? pNRTableModel, bool StatusOfReaccommodation, string ReasonForFailure)
        {
            try
            {
                PNRDetails pNRandAlternateFlightListModel = new PNRDetails
                {
                    pnrDetail = new List<PNRDetail>(),
                    routingDetail = new List<RoutingDetail>()
                };
                if (pNRTableModel != null)
                {
                    PNRDetail pNRTable = new PNRDetail
                    {
                        PNRID = pNRTableModel.PNRID,
                        FLTID = pNRTableModel.FLTID,
                        PNRCode = pNRTableModel.PNRCode,
                        EmailId = pNRTableModel.EmailId,
                        ContactNo = pNRTableModel.ContactNo,
                        RECOStatus = StatusOfReaccommodation ? Enum_PNR.Success.ToString() : Enum_PNR.Failed.ToString(),
                        ReasonForFailure = ReasonForFailure,
                        Priority = pNRTableModel.Priority,
                        ModifiedBy = pNRTableModel.ModifiedBy,
                    };
                    pNRandAlternateFlightListModel.pnrDetail.Add(pNRTable);
                }
                var oldJourneys = oldbooking?.Data?.Journeys ?? new List<Models.ResponseModel.BookingModel.Journey>();
                var newJourneys = newbooking?.Data?.Journeys ?? new List<Models.ResponseModel.BookingModel.Journey>();
                var NoChange = newJourneys.Where(nj => oldJourneys.Any(oj => oj.JourneyKey == nj.JourneyKey)).ToList();
                await AddJourneySegmentsToModel(NoChange, "NoChange", pNRTableModel, pNRandAlternateFlightListModel);
                var Disrupted = oldJourneys.Where(oj => !newJourneys.Any(nj => nj.JourneyKey == oj.JourneyKey)).ToList();
                await AddJourneySegmentsToModel(Disrupted, "Disrupted", pNRTableModel, pNRandAlternateFlightListModel);
                var Alternate = newJourneys.Where(nj => !oldJourneys.Any(oj => oj.JourneyKey == nj.JourneyKey)).ToList();
                await AddJourneySegmentsToModel(Alternate, "Alternate", pNRTableModel, pNRandAlternateFlightListModel);
                return pNRandAlternateFlightListModel;
            }
            catch(Exception ex)
            {
                await _logHelper.LogError($"{":- GetAccommodationByFlightNoResponseModel"} :- {ex.Message}");
                return new PNRDetails
                {
                    pnrDetail = new List<PNRDetail>(),
                    routingDetail = new List<RoutingDetail>()
                };
            }
        }
        async Task AddJourneySegmentsToModel(IEnumerable<Models.ResponseModel.BookingModel.Journey> journeys, string flightBookingType, PNRDetail? pNRTableModel, PNRDetails pNRandAlternateFlightListModel)
        {
            if (journeys != null && journeys.Count() > 0)
            {
                foreach (var journey in journeys)
                {
                    foreach (var segment in journey.Segments)
                    {
                        var alternateAndExistingFlightModel = new RoutingDetail
                        {
                            PNRID = pNRTableModel.PNRID,
                            FlightNumber = segment?.Identifier?.Identifier,
                            AirlineCode = segment?.Identifier?.CarrierCode,
                            FlightDate = segment?.Designator?.Departure,
                            Origin = segment?.Designator?.Origin,
                            Destination = segment?.Designator?.Destination,
                            STD = segment?.Designator?.Departure,
                            DepTerminal= segment?.Legs?[0].LegInfo?.DepartureTerminal,
                            ArrTerminal = segment?.Legs?[0].LegInfo?.ArrivalTerminal,
                            ETD = null,
                            STA = segment?.Designator?.Arrival,
                            ETA = null,
                            FlightBooking = flightBookingType,
                            CreatedBy = pNRTableModel.ModifiedBy,
                        };

                        pNRandAlternateFlightListModel.routingDetail.Add(alternateAndExistingFlightModel);
                    }
                }
            }
        }

    }
}
