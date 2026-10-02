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
    public class AuthController : Controller
    {
        private readonly CoachPulseDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AuthController(
            CoachPulseDbContext context,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (string.IsNullOrWhiteSpace(request.TenantName))
                return BadRequest("Tenant name is required.");

            if (string.IsNullOrWhiteSpace(request.TenantSlug))
                return BadRequest("Tenant slug is required.");

            if (request.Role == UserRole.SuperAdmin)
                return BadRequest("SuperAdmin cannot be registered.");

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
                Role = request.Role
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
                Role = user.Role
            });
        }
    }
}
