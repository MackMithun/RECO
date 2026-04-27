namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Model
{
    public class TripMoveAvailabilityRequest
    {
        public string? journeyKey { get; set; }
        public Stations? stations { get; set; }
        public Dates? dates { get; set; }
        public FlightFilters? flightFilters { get; set; }
        public int type { get; set; }
        public int passengerMoveType { get; set; }
    }
    public class Stations
    {
        public string[]? destinationStationCodes { get; set; }
        public string[]? originStationCodes { get; set; }
    }
    public class Dates
    {
        public string? beginDate { get; set; }
        public string? endDate { get; set; }
    }
    public class FlightFilters
    {
        public string? carrierCode { get; set; }
        public int maxConnectingFlights { get; set; }
    }
}
