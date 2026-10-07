using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly CoachPulseDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public AuthController(
            CoachPulseDbContext context,
            IPasswordHasher passwordHasher, IJwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;

        }

        [HttpPost("register-tenant")]
        public async Task<IActionResult> Register(RegisterTenantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (string.IsNullOrWhiteSpace(request.TenantName))
                return BadRequest("Tenant name is required.");

            if (string.IsNullOrWhiteSpace(request.TenantSlug))
                return BadRequest("Tenant slug is required.");

      

            var email = request.Email.Trim().ToLower();

            var existingUser = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Email == email);

            if (existingUser != null)
                return BadRequest("Email already exists.");

            var existingTenant = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Slug == request.TenantSlug);

            if (existingTenant != null)
                return BadRequest("Tenant slug already exists.");

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.TenantName.Trim(),
                Slug = request.TenantSlug.Trim().ToLower(),
                Status = "Active"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = UserRole.Owner
            };

            _context.Tenants.Add(tenant);
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Registration successful.",
                UserId = user.Id,
                TenantId = tenant.Id,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            var email = request.Email.Trim().ToLower();

            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
                return Unauthorized("Invalid email or password.");

            var passwordValid = _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);

            if (!passwordValid)
                return Unauthorized("Invalid email or password.");

            if (user.TenantId.HasValue)
            {
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(x => x.Id == user.TenantId.Value);

                if (tenant == null)
                    return Unauthorized("Tenant not found.");

                if (!string.Equals(
                        tenant.Status,
                        "Active",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized("Tenant is not active.");
                }
            }

            var accessToken = _jwtService.GenerateAccessToken(user);

            return Ok(new
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresInMinutes = 60,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.TenantId,
                    user.Role
                }
            });
        }
    }
}
