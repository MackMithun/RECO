
namespace RECO.Reaccommodation_MS.ReaccommodationService.Area.Model
{
    public class QueueModel
    {
        public string QueueCode { get; set; }
        public string oldQueue { get; set; }
        public string BookingComment { get; set; }
        public string CommitComment { get; set; }
        public string? decryptedPassword;
        public string Notes { get; set; }
        public string? Password
        {
            get
            {
                return decryptedPassword == null ? null : Encrypt.MsDecrypt(decryptedPassword);
            }
            set { decryptedPassword = value; }
        }
        public string AuthorizedBy { get; set; }
    }
    public class CreateQueueRequest
    {
        public string QueueCode { get; set; }
        public string Notes { get; set; }
        public string? decryptedPassword;
        public string? Password
        {
            get
            {
                return decryptedPassword == null ? null : Encrypt.MsDecrypt(decryptedPassword);
            }
            set { decryptedPassword = value; }
        }
    }
    public class DeleteQueueRequest
    {
        public string? decryptedPassword;
        public string QueueCode { get; set; }
        public string Notes { get; set; }
        public string? Password
        {
            get
            {
                return decryptedPassword == null ? null : Encrypt.MsDecrypt(decryptedPassword);
            }
            set { decryptedPassword = value; }
        }
        public string AuthorizedBy { get; set; }
    }
}
