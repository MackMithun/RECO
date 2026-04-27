namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Model
{
    public class UpdateJourneyModel
    {
        public string? fromJourneyKey { get; set; }
        public string? toJourneyKey { get; set; }
        public string? fareKey { get; set; }
        public string? standbyPriorityCode { get; set; }
        public int inventoryControlType { get; set; }
        public int changeReasonCode { get; set; }
        public string? bookingComment { get; set; }
        public bool adHocConnection { get; set; } = true;
        public bool adHocIsForGeneralUse { get; set; } = true;
    }
}
