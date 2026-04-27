using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class Delay_ToJourneyKey : IDelay_ToJourneyKey
    {
        private readonly ILogHelper _logHelper;
        public Delay_ToJourneyKey(ILogHelper logHelper)
        {
            _logHelper = logHelper;
        }
        public async Task<ToJourneyDetails?> GetJourneyKey(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {  
                Dictionary<int, List<ToJourneyDetails>>? getAllTheTojournkey = new Dictionary<int, List<ToJourneyDetails>>();
                if(reaccommodationMs?.tripMoveAvailability?.Data?.Results?[0].Trips?[0].JourneysAvailableByMarket?.Values is not null)
                {
                    getAllTheTojournkey = await FindAllEligibleJourneyKeys(reaccommodationMs);
                    if(getAllTheTojournkey is not null && getAllTheTojournkey?.Values.Count() > 0)
                    {
                        return await ValidateGetAlltoJourney(reaccommodationMs.sortedPNR, reaccommodationMs?.FlightPriorityRules, getAllTheTojournkey);
                    } 
                }
                return null;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"ToJourneyKey PNR :" + reaccommodationMs?.sortedPNR?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<Dictionary<int, List<ToJourneyDetails>>?> FindAllEligibleJourneyKeys(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                Dictionary<int, List<ToJourneyDetails>> getAllTheTojournkey = new Dictionary<int, List<ToJourneyDetails>>();
                foreach (var _numberOfFlight in reaccommodationMs?.tripMoveAvailability?.Data?.Results?[0].Trips?[0].JourneysAvailableByMarket?.Values)
                {
                    foreach (var item in _numberOfFlight)
                    {
                        try
                        {
                            string newIdentifier = item?.Segments?[0].Identifier?.identifier ?? "";
                            if (!reaccommodationMs.NoMoveOnToList.Contains(newIdentifier) && !string.IsNullOrEmpty(newIdentifier))
                            {
                                int _flagCount = Convert.ToInt32(reaccommodationMs?.AppParameterList?["MaxSeatLeft"]);
                                int availableCount = item?.Fares?[0].Details?[0].AvailableCount ?? 0;
                                if ((availableCount - reaccommodationMs?.PassengersCount) > _flagCount)
                                {
                                    string? presentIdentifier = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Segments?[0].Identifier?.Identifier ?? "";
                                    if (newIdentifier != presentIdentifier)
                                    {
                                        //check MCT
                                        if (item?.Designator.Departure > reaccommodationMs?.impactedFlight?.NearestJourneySAT && item.Designator.Arrival < reaccommodationMs.impactedFlight.NearestJourneySDT)
                                        {
                                            ToJourneyDetails? toJourneyDetails = await EligibilToJourneyKey(item, reaccommodationMs);
                                            if (toJourneyDetails is not null)
                                            {
                                                if (!getAllTheTojournkey.Keys.Contains(item.FlightType))
                                                {
                                                    getAllTheTojournkey.Add(item.FlightType, new List<ToJourneyDetails>
                                                    {
                                                        toJourneyDetails
                                                    });
                                                }
                                                else
                                                {
                                                    getAllTheTojournkey[item.FlightType].Add(toJourneyDetails);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                return getAllTheTojournkey;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"ToJourneyKey PNR :" + reaccommodationMs?.sortedPNR?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<ToJourneyDetails?> EligibilToJourneyKey(Area.Model.Journey? item , Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                int PreponMin = Convert.ToInt32(reaccommodationMs?.AppParameterList?["PreponMin"]);
                DateTime scheduledDepartureTimes = (DateTime)reaccommodationMs.bookingDetails.Data.Journeys[0].Designator.Departure;
                scheduledDepartureTimes = scheduledDepartureTimes.AddMinutes(-PreponMin); // min preprod time

                int PostpondMAX = Convert.ToInt32(reaccommodationMs?.AppParameterList?["MinimumDelay"]);
                DateTime? _NewAlternateFlightDepartureDateTime = item.Designator.Departure;
                DateTime? _NewAlternateFlightArrivalTime = item.Designator.Arrival;
                DateTime _discruptedFlightArrivalTime = (DateTime)reaccommodationMs.bookingDetails.Data.Journeys[0].Designator.Arrival;
                _discruptedFlightArrivalTime = _discruptedFlightArrivalTime.AddMinutes(PostpondMAX); // max postpont time  
                if (_NewAlternateFlightArrivalTime < _discruptedFlightArrivalTime && _NewAlternateFlightDepartureDateTime > scheduledDepartureTimes)
                {
                    return await GetToJourneykey(item);
                }
                return null;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"ToJourneyKey PNR :" + reaccommodationMs?.sortedPNR?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<ToJourneyDetails?> GetToJourneykey(Area.Model.Journey? item)
        {
            return new ToJourneyDetails
            {
                JourneyKey = item?.JourneyKey??"",
                newIdentifier = item?.Segments?[0].Identifier?.identifier ?? "",
                FlightType = item?.FlightType,
                NewFlightDepartureDateTime = item?.Designator.Departure,
                NewFlighttArriavalTime = item?.Designator.Arrival,
                NewOriginStation = item?.Designator?.Origin??"",
                DestinationStation = item?.Designator?.Destination??"",
            };
        }
        private async Task<ToJourneyDetails> ValidateGetAlltoJourney(SortedPNR? listOFPNRDetails, Dictionary<string, List<int>>? Alternateflightpriority, Dictionary<int, List<ToJourneyDetails>> getAllTheTojournkey)
        {
            if (Alternateflightpriority.ContainsKey(listOFPNRDetails.FlightType.ToString()))
            {
                if (getAllTheTojournkey.ContainsKey(Alternateflightpriority[listOFPNRDetails.FlightType.ToString()][0]))
                {
                    return getAllTheTojournkey[listOFPNRDetails.FlightType][0];
                }
                else if (getAllTheTojournkey.ContainsKey(Alternateflightpriority[listOFPNRDetails.FlightType.ToString()][1]))
                {
                    return getAllTheTojournkey[listOFPNRDetails.FlightType][0];
                }
                else
                {
                    return getAllTheTojournkey.OrderBy(x => x.Key).FirstOrDefault().Value[0];
                }
            }
            else
            {
                return getAllTheTojournkey.OrderBy(x => x.Key).FirstOrDefault().Value[0];
            }
        }
    }
}
