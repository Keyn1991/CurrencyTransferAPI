// Plik: Services/AuthService.cs

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
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace CurrencyTransferAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest model)
        {
            if (await _context.Users.AnyAsync(u => u.Username == model.Username || u.Email == model.Email))
            {
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
            
            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest model)
        {
            var loginIdentifier = model.Username ?? model.Email;
            
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == loginIdentifier || u.Email == loginIdentifier);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return null;
            }

            return GenerateAuthResponse(user);
        }

        private AuthResponse GenerateAuthResponse(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            
            var jwtKey = _configuration["Jwt:Key"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username,
                // POPRAWKA: Przypisujemy wartość do pola 'Roles' (liczba mnoga).
                Roles = new List<string> { user.Role }
            };
        }
        
        public async Task SeedAdminUserAsync()
        {
            if (!await _context.Users.AnyAsync(u => u.Role == UserRoles.Admin))
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123!"),
                    Role = UserRoles.Admin
                };
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
            }
        }
    }
}
