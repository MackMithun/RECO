using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey
{
    public class ToJourneyKeyService : IToJourneyKeyService
    {
        private readonly ILogHelper _logHelper;
        private readonly ICancelled_ToJourneyKey _cancelled_ToJourneyKey;
        private readonly IDelay_ToJourneyKey _delay_ToJourneyKey;
        public ToJourneyKeyService(ILogHelper logHelper, ICancelled_ToJourneyKey cancelled_ToJourneyKey, IDelay_ToJourneyKey delay_ToJourneyKey)
        {
            _logHelper = logHelper;
            _cancelled_ToJourneyKey = cancelled_ToJourneyKey;   
            _delay_ToJourneyKey= delay_ToJourneyKey;
        }
        public async Task<ToJourneyDetails?> GetKeyJourneyNavigateFlightDate(Reaccommodation_Model? reaccommodationMs, bool NextTry)
        {
            try
            {
                if (reaccommodationMs?.disruptedFlightDB?.DisruptionType?.ToLower().Trim() == Enum_DisruptionType.cancelled.ToString().ToLower()) //_filghtStatus "Canceled" ||  "Delay"
                {
                    return await _cancelled_ToJourneyKey.cancelGetKeyJourneyNavigateFlightDate(reaccommodationMs,NextTry);
                }
                else if (reaccommodationMs?.disruptedFlightDB?.DisruptionType?.ToLower().Trim() == Enum_DisruptionType.delayed.ToString().ToLower() || reaccommodationMs?.disruptedFlightDB?.DisruptionType?.ToLower().Trim() == Enum_DisruptionType.advanced.ToString().ToLower())//_filghtStatus  "Canceled" ||  "Delay" ||Advanced
                {
                    return await delayGetKeyJourneyNavigateFlightDate(reaccommodationMs);
                }
                else
                {
                    throw new InvalidOperationException("Check the flight status to see whether it is canceled or delayed.");
                }

            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"GetKeyJourneyNavigateFlightDate PNR :" + reaccommodationMs?.PNRDetail?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                await _logHelper.LogConsoleException(ex);
                return null;
            }
        }
        private async Task<ToJourneyDetails> delayGetKeyJourneyNavigateFlightDate(Reaccommodation_Model? reaccommodationMs)
        {
            try
            {
                reaccommodationMs.impactedFlight = await reaccommodationMs.dataMsService.GetStationConnectionRuleList(reaccommodationMs.impactedFlight);
                if (reaccommodationMs?.disruptedFlightDB?.ETA != null && reaccommodationMs.disruptedFlightDB.ETD != null)
                {
                    ToJourneyDetails? toJourneyDetails =await _delay_ToJourneyKey.GetJourneyKey(reaccommodationMs);
                    if(toJourneyDetails is not null && !string.IsNullOrEmpty(toJourneyDetails.JourneyKey))
                    {
                        return toJourneyDetails;        
                    }
                    else
                    {
                        throw new InvalidOperationException(Enum_PNR.NoFlightAvailable.ToString());
                    }
                }
                else
                {
                    throw new InvalidOperationException("The estimated arrival and departure times are null.");
                }
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{"delayGetKeyJourneyNavigateFlightDate PNR :" + reaccommodationMs?.sortedPNR?.PNRCode ?? ""} :- {"End msg :" + ex.Message}");
                throw new InvalidOperationException(ex.Message);

            }
        }
    }
}
