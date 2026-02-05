using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PurrVet.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PurrVet.Infrastructure {
    public class JwtTokenService {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public JwtTokenService(IConfiguration config, ApplicationDbContext context) {
            _config = config;
            _context = context;
        }

        public string GenerateAccessToken(User user, int ownerId) {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

            var claims = new List<Claim> {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, "Owner"),
                new("name", $"{user.FirstName} {user.LastName}"),
                new("ownerId", ownerId.ToString()),
                new("profileImage", user.ProfileImage ?? "pet.png")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken() {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));

            var validationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = key
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)) {
                return null;
            }

            return principal;
        }

        public async Task<RefreshToken> SaveRefreshTokenAsync(int userId, string token, string? deviceInfo) {
            var refreshExpiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "30");

            var refreshToken = new RefreshToken {
                UserID = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = deviceInfo
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<(bool success, string? newAccessToken, string? newRefreshToken, DateTime? expiresAt, string? error)>
            RotateRefreshTokenAsync(string oldTokenStr) {
            var oldToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == oldTokenStr);

            if (oldToken == null)
                return (false, null, null, null, "Invalid refresh token.");

            // Token reuse detection: if already revoked, revoke ALL tokens for this user
            if (oldToken.IsRevoked) {
                var allUserTokens = await _context.RefreshTokens
                    .Where(r => r.UserID == oldToken.UserID && r.RevokedAt == null)
                    .ToListAsync();

                foreach (var t in allUserTokens)
                    t.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return (false, null, null, null, "Token reuse detected. All sessions revoked.");
            }

            if (oldToken.IsExpired)
                return (false, null, null, null, "Refresh token has expired.");

            var user = oldToken.User;
            if (user == null || user.Status != "Active" || user.Type != "Owner")
                return (false, null, null, null, "User account is inactive or unauthorized.");

            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.UserID == user.UserID);
            if (owner == null)
                return (false, null, null, null, "Owner record not found.");

            // Revoke old token
            oldToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(user, owner.OwnerID);
            var newRefreshTokenStr = GenerateRefreshToken();

            oldToken.ReplacedByToken = newRefreshTokenStr;

            await SaveRefreshTokenAsync(user.UserID, newRefreshTokenStr, oldToken.DeviceInfo);

            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            return (true, newAccessToken, newRefreshTokenStr, expiresAt, null);
        }

        public async Task RevokeRefreshTokenAsync(string tokenStr) {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == tokenStr);

            if (token != null && !token.IsRevoked) {
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
