using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class PaxPriorityService : IPaxPriorityService
    {
        private ILogHelper _logger;     
        public PaxPriorityService(ILogHelper logger) { 
        _logger = logger;   
        }
        public async Task<bool> LOY_GRP(BookingDetails bookingDetails)
        {
            try
            {
                var program = bookingDetails?.Data?.Passengers?.Any(x => x.Value?.program != null) ?? false;
                if(program)
                {
                    var Group = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                         journey.Segments.Any(segment => segment.Fares != null && segment.Fares.Any(fare => fare.ProductClass?.ToUpper().Trim() == "G")))??false;
                    if(!Group)
                    {
                        program = false;
                    }
                }
                return program;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- LOY_GRP"} :- {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LOY_SSRWCHR(BookingDetails bookingDetails)
        {
            try
            {
                var program = bookingDetails?.Data?.Passengers?.Any(x => x.Value?.program != null) ?? false;
                if (program)
                {
                    var WCHRSSR = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                        journey.Segments.Any(segment => segment.passengerSegment != null && 
                        segment.passengerSegment.Any(passenger => passenger.Value?.ssrs != null &&
                        passenger.Value.ssrs.Any(ssr => string.Equals(ssr.ssrCode?.Trim(), "WCHR", StringComparison.OrdinalIgnoreCase)))))??false;
                    if (!WCHRSSR)
                    {
                        program = false;
                    }
                }
                return program;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- LOY_SSRWCHR"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> LOY_SSRFamily(BookingDetails bookingDetails)
        {
            try
            {
                var program = bookingDetails?.Data?.Passengers?.Any(x => x.Value?.program != null) ?? false;
                if (program)
                {
                    var Group = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                         journey.Segments.Any(segment => segment.Fares != null && segment.Fares.Any(fare => fare.ProductClass?.ToUpper().Trim() == "A")))??false;
                    if (!Group)
                    {
                        program = false;
                    }
                }
                return program;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- LOY_SSRFamily"} :- {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LOY(BookingDetails bookingDetails)
        {
            try
            {
                var program = bookingDetails?.Data?.Passengers?.Any(x => x.Value?.program != null) ?? false;
                return program;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- LOY"} :- {ex.Message}");
                return false;
            }
        }

        public async Task<bool> WCHR(BookingDetails bookingDetails)
        {
            try
            {
                var WCHRSSR =bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                journey.Segments.Any(segment => segment.passengerSegment != null &&
                segment.passengerSegment.Any(passenger => passenger.Value?.ssrs != null &&
                passenger.Value.ssrs.Any(ssr => string.Equals(ssr.ssrCode?.Trim(), "WCHR", StringComparison.OrdinalIgnoreCase)))))??false;          
                return WCHRSSR;     
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- WCHR"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UMNR(BookingDetails bookingDetails)
        {
            try
            {
                var UMNRSSR = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                journey.Segments.Any(segment => segment.passengerSegment != null &&
                segment.passengerSegment.Any(passenger => passenger.Value?.ssrs != null &&
                passenger.Value.ssrs.Any(ssr => string.Equals(ssr.ssrCode?.Trim(), "UMNR", StringComparison.OrdinalIgnoreCase)))))??false;
                return UMNRSSR;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- UMNR"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> GROUP(BookingDetails bookingDetails)
        {
            try
            {     
                var Group = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                        journey.Segments.Any(segment => segment.Fares != null && segment.Fares.Any(fare => fare.ProductClass?.ToUpper().Trim() == "G")))??false;
                return Group; 
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- GROUP"} :- {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SME(BookingDetails bookingDetails)
        {
            try
            {
                var SME = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                        journey.Segments.Any(segment => segment.Fares != null && segment.Fares.Any(fare => fare.ProductClass?.ToUpper().Trim() == "M")))??false;
                return SME;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- SME"} :- {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CORPORATE(BookingDetails bookingDetails)
        {
            try
            {
                var corporate = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                        journey.Segments.Any(segment => segment.Fares != null && segment.Fares.Any(fare => fare.ProductClass?.ToUpper().Trim() == "F")))??false;
                return corporate;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- CORPORATE"} :- {ex.Message}");
                return false;
            }
        }
        public async Task<bool> Infant(BookingDetails bookingDetails)
        {
            try
            {
                var hasInfant = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
           journey.Segments.Any(segment => segment.passengerSegment != null &&
           segment.passengerSegment.Any(passenger => passenger.Value?.hasInfant == true)))??false;
                return hasInfant;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- Infant"} :- {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CHILD(BookingDetails bookingDetails)
        {
            try
            {
                var CHILD = bookingDetails?.Data?.Passengers?.Any(PS => PS.Value?.PassengerTypeCode?.ToUpper()?.Trim() == "CHD") ?? false;
                return CHILD;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- CHILD"} :- {ex.Message}");
                return false;
            }

        }

        public async Task<bool> FAMILY(BookingDetails bookingDetails)
        {
            try
            {
                var FAMILY = bookingDetails?.Data?.Journeys?.Any(journey => journey.Segments != null &&
                        journey.Segments.Any(segment => segment.Fares != null && segment.Fares.Any(fare => fare.ProductClass?.ToUpper().Trim() == "A")))??false;
                return FAMILY;
            }
            catch (Exception ex)
            {
                await _logger.LogError($"{" :- FAMILY"} :- {ex.Message}");
                return false;
            }
        }

    }
}
