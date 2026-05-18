using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API_PAYSIM.Helpers
{
    public class JwtHelper
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtHelper(IConfiguration configuration)
        {
            _secretKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt key is not set");
            _issuer = "My_issuer-_number--4258ff";
            _audience = "My-audience-_numberPrimarycode__4560ML5P7";
        }
        public string GenerateToken(Guid id, string? nom)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, nom!),
                new Claim("UserId",id.ToString())
            };

            var tokent = new JwtSecurityToken(
                issuer: "My_issuer-_number--4258ff",
                audience: "My-audience-_numberPrimarycode__4560ML5P7",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(tokent);
        }




        public string GenerateTokenChat(string id)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim("UserId",id.ToString())
            };

            var tokent = new JwtSecurityToken(
                issuer: "My_issuer-_number--4258ff",
                audience: "My-audience-_numberPrimarycode__4560ML5P7",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(tokent);
        }

        // Valide le token et retourne un ClaimsPrincipal si valide, sinon null
        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null; // invalide ou expiré
            }
        }
    }
}
