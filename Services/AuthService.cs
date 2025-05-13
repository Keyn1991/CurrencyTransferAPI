using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Dla ILogger

namespace CurrencyTransferAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger; // Dodane pole loggera

        // Zaktualizowany konstruktor do wstrzyknięcia ILogger
        public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger; // Przypisanie loggera
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest model)
        {
            _logger.LogInformation("RegisterAsync called for Username: {Username}", model.Username);
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists.", model.Username);
                return null;
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRoles.User
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {Username} registered successfully with ID: {UserId}", user.Username, user.Id);

            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest model)
        {
            _logger.LogInformation("LoginAsync called for Username: {Username}", model.Username);
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Username} not found.", model.Username);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for User {Username}.", model.Username);
                return null;
            }
            _logger.LogInformation("User {Username} logged in successfully.", user.Username);
            return GenerateAuthResponse(user);
        }

        private AuthResponse GenerateAuthResponse(User user)
        {
            _logger.LogInformation("--- Generating token for User in AuthService ---");
            _logger.LogInformation("Input User - ID: {UserId}, Username: {Username}, Role: {UserRole}, Email: {UserEmail}",
                user.Id, user.Username, user.Role, user.Email);

            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                _logger.LogError("JWT settings (Key, Issuer, or Audience) are not configured properly in appsettings.json.");
                throw new InvalidOperationException("JWT settings are not configured properly.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),                // Subject (nazwa użytkownika)
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),           // ID użytkownika (używamy ClaimTypes.NameIdentifier)
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty), // Email (można też użyć ClaimTypes.Email)
                new Claim(ClaimTypes.Role, user.Role),                              // Rola użytkownika
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())     // JWT ID, unikalny identyfikator tokenu
            };

            _logger.LogInformation("Claims prepared for token generation for User ID {UserId}:", user.Id);
            foreach (var claim in claims)
            {
                _logger.LogInformation("Claim Type: {ClaimType}, Claim Value: {ClaimValue}", claim.Type, claim.Value);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1), // Czas ważności tokenu
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("Token generated successfully for User ID {UserId}.", user.Id);

            return new AuthResponse
            {
                Token = tokenString,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task SeedAdminUserAsync()
        {
            _logger.LogInformation("Attempting to seed admin user.");
            if (!await _context.Users.AnyAsync(u => u.Role == UserRoles.Admin))
            {
                var adminUser = new User
                {
                    Username = _configuration["AdminUser:Username"] ?? "admin", // Odczyt z konfiguracji lub domyślny
                    Email = _configuration["AdminUser:Email"] ?? "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(_configuration["AdminUser:Password"] ?? "AdminPass123!"), // Odczyt z konfiguracji lub domyślny
                    Role = UserRoles.Admin
                };
                // WAŻNE: W środowisku produkcyjnym hasło admina powinno być silne i zarządzane bezpiecznie!
                // Nie trzymaj domyślnych haseł w kodzie produkcyjnym.

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin user seeded successfully with Username: {AdminUsername}", adminUser.Username);
            }
            else
            {
                _logger.LogInformation("Admin user already exists. Seeding skipped.");
            }
        }
    }
}