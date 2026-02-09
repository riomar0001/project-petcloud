using System.Security.Claims;

namespace PetCloud.Infrastructure {
    public static class ClaimsPrincipalExtensions {
        public static int GetUserId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public static int GetOwnerId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirst("ownerId")?.Value ?? "0");

        public static string GetUserName(this ClaimsPrincipal user)
            => user.FindFirst("name")?.Value ?? "Owner";

        public static string GetEmail(this ClaimsPrincipal user)
            => user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}
