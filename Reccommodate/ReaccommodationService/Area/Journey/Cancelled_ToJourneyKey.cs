using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.IUtilities;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class Cancelled_ToJourneyKey : ICancelled_ToJourneyKey
    {
        private readonly ILogHelper _logHelper;
        public Cancelled_ToJourneyKey(ILogHelper logHelper)
        {
            _logHelper = logHelper;
        }
        public async Task<ToJourneyDetails?> cancelGetKeyJourneyNavigateFlightDate(Reaccommodation_Model? reaccommodationMs, bool NextTry)
        {
            try
            {
                reaccommodationMs.impactedFlight = await reaccommodationMs.dataMsService.GetStationConnectionRuleList(reaccommodationMs.impactedFlight);
                Dictionary<int, List<ToJourneyDetails>> getAllTheTojournkey = new Dictionary<int, List<ToJourneyDetails>>();
                int MaxSeatLeft = Convert.ToInt32(reaccommodationMs?.AppParameterList?["MaxSeatLeft"]); // Five Seat is Left
                if (reaccommodationMs?.tripMoveAvailability != null && reaccommodationMs.tripMoveAvailability.Data != null && reaccommodationMs.tripMoveAvailability.Data?.Results?.Count > 0)
                {
                    foreach (var _numberOfFlight in reaccommodationMs?.tripMoveAvailability?.Data?.Results?[0].Trips?[0].JourneysAvailableByMarket?.Values)
                    {
                        foreach (var item in _numberOfFlight)
                        {
                            try
                            {
                                bool _status = item.Segments.Any(x => x.Legs.Any(x => x.LegInfo?.CodeShareIndicator != 0));
                                if (!_status)
                                {
                                    string newIdentifier = item.Segments?[0].Identifier?.identifier ?? "";
                                    if (!reaccommodationMs.NoMoveOnToList.Contains(newIdentifier))
                                    {
                                        ToJourneyDetails? journeyDetails = await CheckTheJourney(item, reaccommodationMs, MaxSeatLeft, newIdentifier, NextTry);
                                        if (journeyDetails != null)
                                        {
                                            if (!getAllTheTojournkey.Keys.Contains(item.FlightType))
                                            {
                                                getAllTheTojournkey.Add(item.FlightType, new List<ToJourneyDetails>
                                            {
                                                journeyDetails
                                            });
                                            }
                                            else
                                            {
                                                getAllTheTojournkey[item.FlightType].Add(journeyDetails);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                continue;
                            }
                            
                        }
                    }
                }
                return await MapToJourney(reaccommodationMs?.sortedPNR, reaccommodationMs?.FlightPriorityRules, getAllTheTojournkey);

            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"cancelGetKeyJourneyNavigateFlightDate PNR :" + reaccommodationMs?.sortedPNR?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<ToJourneyDetails?> CheckTheJourney(Model.Journey JourneysAvailable, Reaccommodation_Model? reaccommodationMs,int MaxSeatLeft,string newIdentifier, bool NextTry)
        {
            try
            {
                if(JourneysAvailable!=null)
                {
                    int availableCount = await GetAvailableCount(JourneysAvailable);
                    if((availableCount - reaccommodationMs?.PassengersCount) > MaxSeatLeft)
                    {
                        if(JourneysAvailable.Designator.Departure > reaccommodationMs?.impactedFlight?.NearestJourneySAT && JourneysAvailable.Designator.Arrival < reaccommodationMs?.impactedFlight.NearestJourneySDT)
                        {
                            if(NextTry)
                            {
                                DateTime? _newIdentiferFlightDepartureDateTime = JourneysAvailable.Designator.Departure;
                                return await bindPostpond(JourneysAvailable, newIdentifier, _newIdentiferFlightDepartureDateTime);
                            }
                            else
                            {
                                DateTime? _cancelFlightDepartureDateTime = reaccommodationMs?.bookingDetails?.Data?.Journeys?[0].Designator?.Departure;
                                DateTime? _newIdentiferFlightDepartureDateTime = JourneysAvailable.Designator.Departure;
                                //prepond
                                if (_cancelFlightDepartureDateTime > _newIdentiferFlightDepartureDateTime)
                                {
                                    int PreponMin = Convert.ToInt32(reaccommodationMs?.AppParameterList?["PreponMin"]);
                                    return await fnprepond(JourneysAvailable, reaccommodationMs, _cancelFlightDepartureDateTime, _newIdentiferFlightDepartureDateTime, PreponMin, newIdentifier);
                                }
                                else if (_cancelFlightDepartureDateTime < _newIdentiferFlightDepartureDateTime)
                                {
                                    return await bindPostpond(JourneysAvailable, newIdentifier, _newIdentiferFlightDepartureDateTime);
                                }
                            }
                            
                        }
                    }
                }

                return null;
            }
            catch (Exception ex) {
                await _logHelper.LogError($"{"cancelGetKeyJourneyNavigateFlightDate PNR :"} :- {"End msg :" + ex.Message}");
               return null;
            }
        }
        
        private async Task<ToJourneyDetails?> fnprepond(Model.Journey JourneysAvailable, Reaccommodation_Model? reaccommodationMs, DateTime? _cancelFlightDepartureDateTime, DateTime? _newIdentiferFlightDepartureDateTime, int PreponMin,string newIdentifier)
        {
            try
            {
                TimeSpan? timeDifference = _cancelFlightDepartureDateTime - _newIdentiferFlightDepartureDateTime;              
                if (Math.Abs(((TimeSpan)timeDifference).TotalMinutes) <= PreponMin)
                {
                    return await bindPrepon(JourneysAvailable, newIdentifier, _newIdentiferFlightDepartureDateTime);
                }
                return null;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"fnprepond PNR :" + reaccommodationMs?.sortedPNR?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<ToJourneyDetails?> bindPostpond(Area.Model.Journey? JourneysAvailable, string newIdentifier, DateTime? _newIdentiferFlightDepartureDateTime)
        {
            try
            {
                return new ToJourneyDetails
                {
                    JourneyKey = JourneysAvailable?.JourneyKey,
                    newIdentifier = newIdentifier,
                    FlightType = JourneysAvailable?.FlightType,
                    NewFlightDepartureDateTime = _newIdentiferFlightDepartureDateTime,
                    NewFlighttArriavalTime = JourneysAvailable?.Designator.Arrival,
                    NewOriginStation = JourneysAvailable?.Designator.Origin,
                    DestinationStation = JourneysAvailable?.Designator.Destination,
                };
            }
            catch(Exception ex)
            {
                await _logHelper.LogError($"{"bindPostpond : "} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<ToJourneyDetails?> bindPrepon(Area.Model.Journey? JourneysAvailable,string newIdentifier, DateTime? _newIdentiferFlightDepartureDateTime)
        {
            try
            {
                return new ToJourneyDetails
                {
                    JourneyKey = JourneysAvailable?.JourneyKey,
                    newIdentifier = newIdentifier,
                    FlightType = JourneysAvailable?.FlightType,
                    NewFlightDepartureDateTime = _newIdentiferFlightDepartureDateTime,
                    NewFlighttArriavalTime = JourneysAvailable?.Designator.Arrival,
                    NewOriginStation = JourneysAvailable?.Designator.Origin,
                    DestinationStation = JourneysAvailable?.Designator.Destination,
                };
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"bindPostpond : "} :- {"End msg :" + ex.Message}");
                return null;
            }
        }
        private async Task<ToJourneyDetails?> MapToJourney(SortedPNR? listOFPNR, Dictionary<string, List<int>>? Alternateflightpriority, Dictionary<int, List<ToJourneyDetails>> getAllTheTojournkey)
        {
            try
            {
                if (getAllTheTojournkey.Count > 0)
                {
                    if (Alternateflightpriority.ContainsKey(listOFPNR.FlightType.ToString()))
                    {
                        if (getAllTheTojournkey.ContainsKey(Alternateflightpriority[listOFPNR.FlightType.ToString()][0]))
                        {
                            return getAllTheTojournkey[Alternateflightpriority[listOFPNR.FlightType.ToString()][0]][0];
                        }
                        else if (getAllTheTojournkey.ContainsKey(Alternateflightpriority[listOFPNR.FlightType.ToString()][1]))
                        {
                            return getAllTheTojournkey[Alternateflightpriority[listOFPNR.FlightType.ToString()][1]][0];
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
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"MapToJourney : "} :- {"End msg :" + ex.Message}");
                return null;
            }
           
        }

        private async Task<int> GetAvailableCount(Area.Model.Journey? item)
        {
            try
            {
                if (item != null && item?.Fares != null && item.Fares.Count > 0)
                {
                    return item.Fares?[0].Details?[0].AvailableCount ?? 0;
                }

                return 0;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetAvailableCount PNR :"} :- {"End msg :" + ex.Message}");
                return 0;
            }
        }

    }
}
