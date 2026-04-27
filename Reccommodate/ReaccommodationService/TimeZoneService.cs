namespace RECO.Reaccommodation_MS.ReaccommodationService
{
    public static class TimeZoneService
    {
        private readonly static string TimeZoneIND = "India Standard Time";
        public static DateTime convertToIND(DateTime currentDateTime)
        {
            TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneIND);
            DateTime indiaDateTime = TimeZoneInfo.ConvertTime(currentDateTime, indiaTimeZone);
            return indiaDateTime;
        }
    }
}
