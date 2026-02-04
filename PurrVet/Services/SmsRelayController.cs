using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SmsRelayController : ControllerBase {
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] object payload) {
        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync(
            "https://sms.iprogtech.com/api/v1/message-reminders", payload);

        var result = await response.Content.ReadAsStringAsync();
        return Content(result, "application/json");
    }
}
