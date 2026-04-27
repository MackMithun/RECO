namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Model
{
    public class BookingCommitModel
    {
        public string LatestReceivedBy { get; set; }
        public string LatestReceivedReference { get; set; }
        public bool RestrictionOverride { get; set; }
        public bool WaiveNameFee { get; set; }
        public bool WaivePenalityFee { get; set; }
        public bool WaiveSpoilageFee { get; set; }
        public bool DistributeToContacts { get; set; }
        public bool NotifyContacts { get; set; }
        public List<Comment1> Comments { get; set; }

    }
    public class Comment1
    {
        public int Type { get; set; }
        public string Text { get; set; }
    }
}
