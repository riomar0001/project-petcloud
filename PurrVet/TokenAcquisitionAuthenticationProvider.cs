using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

public class TokenAcquisitionAuthenticationProvider : IAuthenticationProvider
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public TokenAcquisitionAuthenticationProvider(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition;
    }

    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var scopes = new[] { "User.Read", "Calendars.ReadWrite" };
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

        Console.WriteLine($"Access Token: {accessToken}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
    }
}