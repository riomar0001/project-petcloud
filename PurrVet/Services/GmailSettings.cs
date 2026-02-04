namespace PurrVet.Services {
    public class GmailSettings {
        public string Email { get; set; } = "";
        public string AppPassword { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
    }

}
