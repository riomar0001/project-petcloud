namespace PurrVet.Services {
    public class SmsReminderService {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://sms.iprogtech.com/api/v1/message-reminders";
        private const string ApiToken = "28d87bdfeea075d7becacea513c887aabd2a9bfc";
        private const string SenderName = "Happy Paws Veterinary Clinic";

        public SmsReminderService(HttpClient httpClient) {
            _httpClient = httpClient;
        }

        private string FormatPhone(string phone) {
            if (string.IsNullOrWhiteSpace(phone)) return "639776584500";
            phone = phone.Trim();

            if (phone.StartsWith("09"))
                phone = "63" + phone.Substring(1);
            else if (phone.StartsWith("+63"))
                phone = phone.Substring(1);

            return phone;
        }

        public async Task<bool> ScheduleReminder(string phoneNumber, DateTime scheduledAt, string message) {
            try {
                var formattedPhone = FormatPhone(phoneNumber);

                var requestData = new {
                    api_token = ApiToken,
                    sender_name = SenderName,
                    phone_number = formattedPhone,
                    scheduled_at = scheduledAt.ToString("yyyy-MM-dd hh:mm tt"),
                    message = message
                };

                var response = await _httpClient.PostAsJsonAsync(BaseUrl, requestData);

                if (!response.IsSuccessStatusCode) {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[iProg SMS Error] {response.StatusCode}: {error}");
                }

                return response.IsSuccessStatusCode;
            } catch (Exception ex) {
                Console.WriteLine($"[iProg SMS Exception] {ex.Message}");
                return false;
            }
        }
    }
}
