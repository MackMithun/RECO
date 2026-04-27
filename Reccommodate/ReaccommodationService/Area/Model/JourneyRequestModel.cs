namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Model
{
    public class JourneyRequestModel
    {
        public string? FromJourneykey {  get; set; }
        public string? ToJourneykey { get; set; }
        public string? Farekey { get; set; }
        public string? BookingComment { get; set; }
    }
}
