namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Model
{
    public class TripMoveAvailability
    {
        public Data? Data { get; set; }
    }
    public class Data
    {
        public List<Result>? Results { get; set; }
        public FaresAvailable? FaresAvailable { get; set; }
        public string? CurrencyCode { get; set; }
        public bool IncludeTaxesAndFees { get; set; }
        public object? BundleOffers { get; set; }
    }

    public class Result
    {
        public List<Trip>? Trips { get; set; }
    }

    public class Trip
    {
        public bool MultipleOriginStations { get; set; }
        public bool MultipleDestinationStations { get; set; }
        public DateTime? Date { get; set; }
        public Dictionary<string, List<Journey>>? JourneysAvailableByMarket { get; set; }
    }

    public class Journey
    {
        public int FlightType { get; set; }
        public int Stops { get; set; }
        public Designator Designator { get; set; }
        public string? JourneyKey { get; set; }
        public List<Segment>? Segments { get; set; }
        public List<Fare>? Fares { get; set; }
        public bool? NotForGeneralUser { get; set; }
    }

    public class Designator
    {
        public string? Destination { get; set; }
        public string? Origin { get; set; }
        public DateTime? Arrival { get; set; }
        public DateTime? Departure { get; set; }
    }

    public class Segment
    {
        public bool IsChangeOfGauge { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsHosted { get; set; }
        public Designator? Designator { get; set; }
        public bool IsSeatmapViewable { get; set; }
        public string? SegmentKey { get; set; }
        public Identifier? Identifier { get; set; }
        public object? CabinOfService { get; set; }
        public object? ExternalIdentifier { get; set; }
        public bool International { get; set; }
        public int SegmentType { get; set; }
        public List<Leg>? Legs { get; set; }
    }
    public class Identifier
    {
        public string? identifier { get; set; }
        public string? CarrierCode { get; set; }
        public object? OpSuffix { get; set; }
    }

    public class Leg
    {
        public string? LegKey { get; set; }
        public object? OperationsInfo { get; set; }
        public Designator? Designator { get; set; }
        public LegInfo? LegInfo { get; set; }
        public List<object>? Nests { get; set; }
        public List<Ssr>? Ssrs { get; set; }
        public string? SeatmapReference { get; set; }
        public string? FlightReference { get; set; }
    }
    public class LegInfo
    {
        public DateTime? DepartureTimeUtc { get; set; }
        public DateTime? ArrivalTimeUtc { get; set; }
        public int AdjustedCapacity { get; set; }
        public string? ArrivalTerminal { get; set; }
        public int ArrivalTimeVariant { get; set; }
        public int BackMoveDays { get; set; }
        public int Capacity { get; set; }
        public bool ChangeOfDirection { get; set; }
        public int CodeShareIndicator { get; set; }
        public string? DepartureTerminal { get; set; }
        public int DepartureTimeVariant { get; set; }
        public string? EquipmentType { get; set; }
        public string? EquipmentTypeSuffix { get; set; }
        public bool ETicket { get; set; }
        public bool Irop { get; set; }
        public int Lid { get; set; }
        public object? MarketingCode { get; set; }
        public bool MarketingOverride { get; set; }
        public object? OperatedByText { get; set; }
        public object? OperatingCarrier { get; set; }
        public object? OperatingFlightNumber { get; set; }
        public object? OperatingOpSuffix { get; set; }
        public int OutMoveDays { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public DateTime? DepartureTime { get; set; }
        public string? PrbcCode { get; set; }
        public string? ScheduleServiceType { get; set; }
        public int Sold { get; set; }
        public int Status { get; set; }
        public bool SubjectToGovtApproval { get; set; }
    }

    public class Ssr
    {
        public int Available { get; set; }
        public string? SsrNestCode { get; set; }
        public int Lid { get; set; }
        public int Sold { get; set; }
        public int UnitSold { get; set; }
    }

    public class Fare
    {
        public string? FareAvailabilityKey { get; set; }
        public List<Detail>? Details { get; set; }
        public bool IsSumOfSector { get; set; }
    }
    public class Detail
    {
        public int AvailableCount { get; set; }
        public int Status { get; set; }
        public string? Reference { get; set; }
        public object? ServiceBundleSetCode { get; set; }
        public object? BundleReferences { get; set; }
        public object? SsrReferences { get; set; }
    }
    public class FaresAvailable
    {
        public Dictionary<string, FaresAvailableDetail>? Totals { get; set; }
        public bool IsSumOfSector { get; set; }
        public string? FareAvailabilityKey { get; set; }
        public List<Fare>? Fares { get; set; }
    }

    public class FaresAvailableDetail
    {
        public int FareTotal { get; set; }
        public int RevenueTotal { get; set; }
        public int PublishedTotal { get; set; }
        public int LoyaltyTotal { get; set; }
        public int DiscountedTotal { get; set; }
    }
}
