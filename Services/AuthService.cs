using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskManager.API.Data;
using TaskManager.API.Models;

namespace TaskManager.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public User? Register(string username, string email, string password, string role = "Employee", string? organizationName = null, int? organizationId = null)
        {
            if (_context.Users.Any(u => u.Email == email))
                return null;

            int? orgId = null;

            // لو مدير — ننشئ منظمة جديدة
            if (role == "Admin" && !string.IsNullOrEmpty(organizationName))
            {
                var org = new Organization
                {
                    Name = organizationName,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Organizations.Add(org);
                _context.SaveChanges();
                orgId = org.Id;
            }
            // لو موظف — ينتمي لمنظمة موجودة
            else if (role == "Employee" && organizationId.HasValue)
            {
                orgId = organizationId;
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                OrganizationId = orgId
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        public string? Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return GenerateToken(user);
        }

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("OrganizationId", user.OrganizationId?.ToString() ?? ""),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}