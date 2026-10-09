using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CoachPulse.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly CoachPulseDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthController(
            CoachPulseDbContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
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
        [HttpPost("register-client")]
        public async Task<IActionResult> RegisterClient(
    RegisterClientRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (request.TenantId == Guid.Empty)
                return BadRequest("TenantId is required.");

            var email = request.Email.Trim().ToLower();

            // Check tenant
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Id == request.TenantId);

            if (tenant == null)
                return BadRequest("Tenant not found.");

            if (!string.Equals(
                    tenant.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Tenant is not active.");
            }

            // Check email
            var existingUser = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == request.TenantId &&
                    x.Email == email);

            if (existingUser != null)
                return BadRequest(
                    "Email already exists in this tenant.");

            // Check coach if provided
            if (request.CoachId.HasValue)
            {
                var coach = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.CoachId.Value &&
                        x.TenantId == request.TenantId &&
                        (x.Role == UserRole.Owner ||
                         x.Role == UserRole.Staff));

                if (coach == null)
                    return BadRequest(
                        "Invalid coach for this tenant.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = UserRole.Client
            };

            var client = new Client
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = user.Id,
                CoachId = request.CoachId,
                Goals = request.Goals?.Trim(),
                HealthInfo = request.HealthInfo?.Trim()
            };

            _context.Users.Add(user);
            _context.Clients.Add(client);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Client registration successful.",
                UserId = user.Id,
                ClientId = client.Id,
                TenantId = tenant.Id,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }
        [AllowAnonymous]
        [HttpPost("accept-invitation")]
        public async Task<IActionResult> AcceptInvitation(
    AcceptInvitationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Invitation token is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (request.Password.Length < 8)
                return BadRequest("Password must be at least 8 characters.");

            var tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

            var invitation = await _context.ClientInvitations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (invitation == null)
                return BadRequest("Invalid invitation.");

            if (invitation.AcceptedAt.HasValue)
                return BadRequest("Invitation has already been accepted.");

            if (invitation.ExpiresAt <= DateTime.UtcNow)
                return BadRequest("Invitation has expired.");

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Id == invitation.TenantId);

            if (tenant == null ||
                !string.Equals(
                    tenant.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Tenant is not active.");
            }

            var existingUser = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == invitation.TenantId &&
                    x.Email == invitation.Email);

            if (existingUser)
                return BadRequest("An account already exists for this email.");

            var coach = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.Id == invitation.CoachId &&
                    x.TenantId == invitation.TenantId &&
                    (x.Role == UserRole.Owner ||
                     x.Role == UserRole.Staff));

            if (coach == null)
                return BadRequest("Invitation coach is no longer valid.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                Email = invitation.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = UserRole.Client
            };

            var client = new Client
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                UserId = user.Id,
                CoachId = invitation.CoachId
            };

            invitation.AcceptedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            _context.Clients.Add(client);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Invitation accepted successfully.",
                UserId = user.Id,
                ClientId = client.Id,
                TenantId = user.TenantId,
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

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = _refreshTokenService.GenerateToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenType = "Bearer",
                ExpiresInMinutes = 60,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.TenantId,
                    user.Role
                }
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(
            RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("Refresh token is required.");

            var refreshToken = await _context.RefreshTokens
                .IgnoreQueryFilters()
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Token == request.RefreshToken);

            if (refreshToken == null)
                return Unauthorized("Invalid refresh token.");

            if (refreshToken.RevokedAt.HasValue)
                return Unauthorized("Refresh token has been revoked.");

            if (refreshToken.ExpiresAt <= DateTime.UtcNow)
                return Unauthorized("Refresh token has expired.");

            if (refreshToken.User == null)
                return Unauthorized("User not found.");

            // Revoke old refresh token
            refreshToken.RevokedAt = DateTime.UtcNow;

            // Generate new access token
            var newAccessToken =
                _jwtService.GenerateAccessToken(refreshToken.User);

            // Generate new refresh token
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = refreshToken.UserId,
                Token = _refreshTokenService.GenerateToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                TokenType = "Bearer",
                ExpiresInMinutes = 60,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
            });
        }
    }
}