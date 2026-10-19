
using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.DTOs.Coach;
using CoachPulse.Application.DTOs;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/coach")]
[Authorize(Policy = "CoachOnly")]
public class CoachController : ControllerBase
{
    private readonly CoachPulseDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IClientAccessService _clientAccess;
    private readonly IEmailSender _emailSender;

    public CoachController(CoachPulseDbContext context, IWebHostEnvironment environment, IClientAccessService clientAccess, IEmailSender emailSender)
    {
        _context = context;
        _environment = environment;
        _clientAccess = clientAccess;
        _emailSender = emailSender;
    }

    [HttpPost("clients/invite")]
    [Obsolete("Use POST /api/clients/invite.")]
    public async Task<IActionResult> InviteClient(
    CreateClientInvitationRequest request)
    {
        if (Request.Path == "/api/coach/clients/invite")
            RouteDeprecation.AddHeaders(Response, "/api/clients/invite");
        return await InviteClientCore(request);
    }

    [HttpPost("~/api/clients/invite")]
    public Task<IActionResult> InviteClientCanonical(CreateClientInvitationRequest request) => InviteClientCore(request);

    private async Task<IActionResult> InviteClientCore(
    CreateClientInvitationRequest request)
    {
        var userIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        var tenantIdClaim = User.FindFirst("tenantId");

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var coachId) ||
            tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Invalid user or tenant information.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");
        if (request.Name?.Length > 150) return UnprocessableEntity("Invite name must be at most 150 characters.");

        var email = request.Email.Trim().ToLowerInvariant();

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant == null)
            return NotFound("Tenant not found.");

        if (!string.Equals(
            tenant.Status,
            "Active",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Tenant is not active.");
        }

        var existingUser = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Email == email);

        if (existingUser)
            return BadRequest("Email already exists in this tenant.");

        var pendingInvitation = await _context.ClientInvitations
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Email == email &&
                x.AcceptedAt == null &&
                x.ExpiresAt > DateTime.UtcNow);

        if (pendingInvitation)
            return BadRequest("An active invitation already exists for this email.");

        var rawToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var invitation = new ClientInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = coachId,
            Email = email,
            InvitedName = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _context.ClientInvitations.Add(invitation);

        await _context.SaveChangesAsync();
        await _emailSender.SendTemplateAsync(email, EmailTemplateType.ClientInvitation, tenant.DefaultLanguage,
            new Dictionary<string, string> { ["token"] = rawToken, ["name"] = request.Name ?? string.Empty }, HttpContext.RequestAborted);

        return Ok(new
        {
            Message = "Invitation created successfully.",
            invitation.Id,
            invitation.Email,
            invitation.ExpiresAt,
            InvitationToken = _environment.IsDevelopment()
                ? rawToken
                : null
        });
    }
    [HttpGet("clients")]
    [Obsolete("Use GET /api/clients.")]
    [ProducesResponseType(typeof(PagedResult<ClientListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClients([FromQuery] string? status = null, [FromQuery] Guid? coachId = null, [FromQuery] string? search = null, [FromQuery] string? riskLevel = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (Request.Path == "/api/coach/clients")
            RouteDeprecation.AddHeaders(Response, "/api/clients");
        return await GetClientsCore(status, coachId, search, riskLevel, page, pageSize);
    }

    [HttpGet("~/api/clients")]
    [ProducesResponseType(typeof(PagedResult<ClientListItem>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetClientsCanonical([FromQuery] string? status = null, [FromQuery] Guid? coachId = null, [FromQuery] string? search = null, [FromQuery] string? riskLevel = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) => GetClientsCore(status, coachId, search, riskLevel, page, pageSize);

    private async Task<IActionResult> GetClientsCore(string? status, Guid? coachId, string? search, string? riskLevel, int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100) return UnprocessableEntity("page must be at least 1 and pageSize must be between 1 and 100.");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var tenantIdClaim = User.FindFirst("tenantId");
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var userId) ||
            tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId) ||
            roleClaim == null)
        {
            return Unauthorized("Invalid user or tenant information.");
        }

        if (!Enum.TryParse<UserRole>(roleClaim.Value, out var role) || role is not (UserRole.TenantOwner or UserRole.CoachStaff))
        {
            return Forbid();
        }

        var query = _clientAccess.VisibleClients(tenantId, userId, role).AsNoTracking();

        if (coachId.HasValue)
            query = query.Where(x => x.CoachId == coachId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(x => x.User!.Email.Contains(searchTerm) || (x.Goals != null && x.Goals.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ClientStatus>(status, true, out var clientStatus) || !Enum.IsDefined(clientStatus))
                return BadRequest("status must be Active, Paused or Archived.");
            query = query.Where(x => x.Status == clientStatus);
        }

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            var normalizedRisk = riskLevel.Trim().ToLowerInvariant();
            if (normalizedRisk is not ("high" or "medium" or "low"))
                return BadRequest("riskLevel must be low, medium or high.");
            if (normalizedRisk == "high") query = query.Where(x => _context.ChurnRiskLogs.Any(r => r.ClientId == x.Id) && _context.ChurnRiskLogs.Where(r => r.ClientId == x.Id).OrderByDescending(r => r.ComputedAt).Select(r => r.RiskScore).FirstOrDefault() > 0.7m);
            else if (normalizedRisk == "medium") query = query.Where(x => _context.ChurnRiskLogs.Any(r => r.ClientId == x.Id) && _context.ChurnRiskLogs.Where(r => r.ClientId == x.Id).OrderByDescending(r => r.ComputedAt).Select(r => r.RiskScore).FirstOrDefault() >= 0.4m && _context.ChurnRiskLogs.Where(r => r.ClientId == x.Id).OrderByDescending(r => r.ComputedAt).Select(r => r.RiskScore).FirstOrDefault() <= 0.7m);
            else query = query.Where(x => _context.ChurnRiskLogs.Any(r => r.ClientId == x.Id) && _context.ChurnRiskLogs.Where(r => r.ClientId == x.Id).OrderByDescending(r => r.ComputedAt).Select(r => r.RiskScore).FirstOrDefault() < 0.4m);
        }

        var totalCount = await query.CountAsync();
        var clients = await query
            .OrderBy(x => x.User!.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ClientListItem(x.Id, x.UserId, x.User!.Name, x.User.Email, x.CoachId, x.Goals, x.Status,
                _context.ChurnRiskLogs.Where(r => r.ClientId == x.Id).OrderByDescending(r => r.ComputedAt).Select(r => (string?)r.RiskLevel).FirstOrDefault()))
            .ToListAsync();

        return Ok(PagedResult<ClientListItem>.Create(clients, page, pageSize, totalCount));
    }
    [HttpPost("programs")]
    [Obsolete("Use POST /api/programs.")]
    public async Task<IActionResult> CreateProgram(
    CreateProgramRequest request)
    {
        if (Request.Path == "/api/coach/programs") RouteDeprecation.AddHeaders(Response, "/api/programs");
        return await CreateProgramCore(request);
    }

    [HttpPost("~/api/programs")]
    public Task<IActionResult> CreateProgramCanonical(CreateProgramRequest request) => CreateProgramCore(request);

    private async Task<IActionResult> CreateProgramCore(CreateProgramRequest request)
    {
        var userIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        var tenantIdClaim = User.FindFirst("tenantId");
        var roleClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role);

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var userId) ||
            tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId) ||
            roleClaim == null)
        {
            return Unauthorized("Invalid user or tenant information.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Program title is required.");

        if (roleClaim.Value is not ("TenantOwner" or "CoachStaff")) return Forbid();
        if (request.DurationDays is < 1 or > 365) return UnprocessableEntity("DurationDays must be between 1 and 365.");
        Client? client = null;
        if (!request.IsTemplate && request.ClientId.HasValue)
        {
            if (request.ClientId == Guid.Empty) return BadRequest("ClientId must be a valid identifier.");
            client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.ClientId && x.TenantId == tenantId);
            if (client is null) return NotFound("Client not found.");
            if (roleClaim.Value == UserRole.CoachStaff.ToString() && client.CoachId != userId) return NotFound();
        }

        var program = new CoachPulse.Domain.Entities.Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client?.Id,
            CoachId = userId,
            Title = request.Title.Trim(),
            IsTemplate = request.IsTemplate,
            DurationDays = request.DurationDays,
            Status = "Draft"
        };

        _context.Programs.Add(program);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Program created successfully.",
            Program = new
            {
                program.Id,
                program.TenantId,
                program.ClientId,
                program.CoachId,
                program.Title,
                program.IsTemplate,
                program.DurationDays,
                program.RowVersion
            }
        });
    }
    [HttpGet("programs")]
    [Obsolete("Use GET /api/programs.")]
    public async Task<IActionResult> GetPrograms()
    {
        if (Request.Path == "/api/coach/programs") RouteDeprecation.AddHeaders(Response, "/api/programs");
        return await GetProgramsCore();
    }

    [HttpGet("~/api/programs")]
    public Task<IActionResult> GetProgramsCanonical() => GetProgramsCore();

    private async Task<IActionResult> GetProgramsCore()
    {
        var userIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        var tenantIdClaim = User.FindFirst("tenantId");
        var roleClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role);

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var userId) ||
            tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId) ||
            roleClaim == null)
        {
            return Unauthorized("Invalid user or tenant information.");
        }

        var query = _context.Programs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (roleClaim.Value == UserRole.CoachStaff.ToString())
        {
            query = query.Where(x => x.IsTemplate || (x.ClientId == null && x.CoachId == userId) ||
                (x.ClientId != null && _context.Clients.Any(c => c.Id == x.ClientId && c.CoachId == userId)));
        }
        else if (roleClaim.Value != UserRole.TenantOwner.ToString())
        {
            return Forbid();
        }

        var programs = await query
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.ClientId,
                ClientEmail = x.Client != null
                ? x.Client.User!.Email
                : null,
                ClientName = x.Client != null
                ? x.Client.User!.Name
                : null,
                x.CoachId,
                x.IsTemplate,
                x.TenantId,
                x.Status,
                x.DurationDays,
                x.StartDate
            })
            .ToListAsync();

        return Ok(programs);
    }

    [HttpGet("exercises")]
    [Obsolete("Use GET /api/exercises.")]
    public async Task<IActionResult> GetExercises()
    {
        if (Request.Path == "/api/coach/exercises") RouteDeprecation.AddHeaders(Response, "/api/exercises");
        return await GetExercisesCore();
    }

    private async Task<IActionResult> GetExercisesCore()
    {
        var exercises = await _context.Exercises
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.MuscleGroup,
                e.VideoUrl
            })
            .ToListAsync();

        return Ok(exercises);
    }
    [HttpPost("exercises")]
    [Authorize(Policy = "CoachOnly")]
    [Obsolete("Use POST /api/exercises.")]
    public async Task<IActionResult> CreateExercise(
    [FromBody] CreateExerciseRequest request)
    {
        if (Request.Path == "/api/coach/exercises") RouteDeprecation.AddHeaders(Response, "/api/exercises");
        return await CreateExerciseCore(request);
    }

    [HttpPost("~/api/exercises")]
    [Authorize(Policy = "CoachOnly")]
    public Task<IActionResult> CreateExerciseCanonical([FromBody] CreateExerciseRequest request) => CreateExerciseCore(request);

    private async Task<IActionResult> CreateExerciseCore(CreateExerciseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.MuscleGroup))
        {
            return BadRequest(new
            {
                message = "Name and MuscleGroup are required."
            });
        }

        var name = request.Name.Trim();
        var muscleGroup = request.MuscleGroup.Trim();
        if (request.VideoUrl is not null && (!Uri.TryCreate(request.VideoUrl, UriKind.Absolute, out var videoUri) || videoUri.Scheme != Uri.UriSchemeHttps))
            return UnprocessableEntity("VideoUrl must be an absolute HTTPS URL.");
        var tenantId = Guid.TryParse(User.FindFirst("tenantId")?.Value, out var parsedTenant) ? parsedTenant : (Guid?)null;
        if (request.VideoFileId is not null && (tenantId is null || !await _context.StoredFiles.AnyAsync(f => f.Id == request.VideoFileId && f.TenantId == tenantId && f.Purpose == "exercise-video" && !f.IsDeleted)))
            return UnprocessableEntity("videoFileId must identify an exercise-video file in this tenant.");

        var exercise = new CoachPulse.Domain.Entities.Exercise
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            MuscleGroup = muscleGroup,
            Equipment = request.Equipment?.Trim(),
            VideoFileId = request.VideoFileId,
            VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl)
                ? null
                : request.VideoUrl.Trim()
        };

        _context.Exercises.Add(exercise);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Exercise created successfully.",
            exercise.Id,
            exercise.Name,
            exercise.MuscleGroup,
            exercise.VideoUrl
        });
    }
    [HttpPost("programs/{programId:guid}/exercises")]
    [Obsolete("Use POST /api/programs/{id}/exercises.")]
    public async Task<IActionResult> AddExerciseToProgram(
        Guid programId,
        [FromBody] AddProgramExerciseRequest request)
    {
        if (Request.Path.StartsWithSegments("/api/coach/programs")) RouteDeprecation.AddHeaders(Response, $"/api/programs/{programId}/exercises");
        return await AddExerciseToProgramCore(programId, request);
    }

    [HttpPost("~/api/programs/{programId:guid}/exercises")]
    public Task<IActionResult> AddExerciseToProgramCanonical(Guid programId, [FromBody] AddProgramExerciseRequest request) => AddExerciseToProgramCore(programId, request);

    private async Task<IActionResult> AddExerciseToProgramCore(Guid programId, AddProgramExerciseRequest request)
    {
        if (request.ExerciseId == Guid.Empty ||
            request.Sets is < 1 or > 20 ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.Reps, @"^\d{1,3}(-\d{1,3})?$") ||
            request.Day <= 0 || request.RestSeconds is < 0 or > 600)
        {
            return BadRequest(new
            {
                message = "ExerciseId, Sets, Reps and Day must be valid."
            });
        }

        var userIdValue = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = User.FindFirst("tenantId")?.Value;

        var role = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized();
        }

        // Load the program within the current tenant.
        var program = await _context.Programs
            .FirstOrDefaultAsync(p =>
                p.Id == programId &&
                p.TenantId == tenantId);

        if (program is null)
        {
            return NotFound(new { message = "Program not found." });
        }

        if (program.Status == "Archived") return Conflict("This program is not editable.");
        if (request.Day > program.DurationDays) return UnprocessableEntity("Day must be within the program duration.");

        // Staff can manage only their own programs.
        if (role == "CoachStaff" && !program.IsTemplate && (program.ClientId is not Guid assignedClient || !await _context.Clients.AnyAsync(c => c.Id == assignedClient && c.CoachId == userId && c.TenantId == tenantId)))
        {
            return NotFound();
        }

        // Shared exercise library: exercises have no TenantId.
        var exerciseExists = await _context.Exercises
            .AnyAsync(e => e.Id == request.ExerciseId && (e.TenantId == null || e.TenantId == tenantId));

        if (!exerciseExists)
        {
            return BadRequest(new
            {
                message = "Exercise does not exist."
            });
        }

        var programExercise = new
            CoachPulse.Domain.Entities.ProgramExercise
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            ExerciseId = request.ExerciseId,
            Sets = request.Sets,
            Reps = request.Reps,
            RestSeconds = request.RestSeconds,
            Day = request.Day
        };

        _context.ProgramExercises.Add(programExercise);
        program.RowVersion = BitConverter.GetBytes(BitConverter.ToInt64(program.RowVersion) + 1);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Exercise added to program successfully.",
            programExercise.Id,
            programExercise.ProgramId,
            programExercise.ExerciseId,
            programExercise.Sets,
            programExercise.Reps,
            programExercise.Day
        });
    }

    [HttpGet("programs/{programId:guid}/exercises")]
    [Obsolete("Use GET /api/programs/{id}/exercises.")]
    public async Task<IActionResult> GetProgramExercises(Guid programId)
    {
        if (Request.Path.StartsWithSegments("/api/coach/programs")) RouteDeprecation.AddHeaders(Response, $"/api/programs/{programId}/exercises");
        return await GetProgramExercisesCore(programId);
    }

    [HttpGet("~/api/programs/{programId:guid}/exercises")]
    public Task<IActionResult> GetProgramExercisesCanonical(Guid programId) => GetProgramExercisesCore(programId);

    private async Task<IActionResult> GetProgramExercisesCore(Guid programId)
    {
        var userIdValue = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = User.FindFirst("tenantId")?.Value;

        var role = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized();
        }

        var program = await _context.Programs
            .FirstOrDefaultAsync(p =>
                p.Id == programId &&
                p.TenantId == tenantId);

        if (program is null)
        {
            return NotFound(new { message = "Program not found." });
        }

        if (role == "CoachStaff" && !program.IsTemplate && (program.ClientId is not Guid assignedClient || !await _context.Clients.AnyAsync(c => c.Id == assignedClient && c.CoachId == userId && c.TenantId == tenantId)))
        {
            return NotFound();
        }

        var exercises = await _context.ProgramExercises
            .Where(pe => pe.ProgramId == programId)
            .Select(pe => new
            {
                pe.Id,
                pe.ExerciseId,
                ExerciseName = pe.Exercise!.Name,
                pe.Exercise.MuscleGroup,
                pe.Sets,
                pe.Reps,
                pe.Day,
                pe.SortOrder
            })
            .OrderBy(pe => pe.Day)
            .ThenBy(pe => pe.SortOrder)
            .ToListAsync();

        return Ok(exercises);
    }

    // POST: api/coach/programs/templates
    // Create a reusable program template.
    [HttpPost("programs/templates")]
    [Obsolete("Use POST /api/programs/{id}/save-as-template.")]
    public async Task<IActionResult> CreateProgramTemplate(
        [FromBody] CreateProgramRequest request)
    {
        if (Request.Path == "/api/coach/programs/templates") RouteDeprecation.AddHeaders(Response, "/api/programs/{id}/save-as-template");
        return await CreateProgramTemplateCore(request);
    }

    [HttpPost("~/api/programs/{id:guid}/save-as-template")]
    public Task<IActionResult> CreateProgramTemplateCanonical(Guid id, [FromBody] CreateProgramRequest request) => CreateProgramTemplateFromProgramCore(id, request.Title);

    private async Task<IActionResult> CreateProgramTemplateFromProgramCore(Guid id, string? title)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = User.FindFirstValue("tenantId");
        if (!Guid.TryParse(userId, out var coachId) || !Guid.TryParse(tenantId, out var tid)) return Unauthorized();
        var source = await _context.Programs.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsTemplate);
        if (source is null) return NotFound();
        if (User.FindFirstValue(ClaimTypes.Role) == UserRole.CoachStaff.ToString() &&
            (source.ClientId is Guid cid ? !await _context.Clients.AnyAsync(c => c.Id == cid && c.CoachId == coachId && c.TenantId == tid) : source.CoachId != coachId)) return NotFound();
        var template = new CoachPulse.Domain.Entities.Program { Id = Guid.NewGuid(), TenantId = tid, CoachId = coachId, Title = string.IsNullOrWhiteSpace(title) ? source.Title : title.Trim(), IsTemplate = true, Status = "Draft", DurationDays = source.DurationDays };
        _context.Programs.Add(template);
        var exercises = await _context.ProgramExercises.AsNoTracking().Where(x => x.ProgramId == id).ToListAsync();
        foreach (var exercise in exercises) _context.ProgramExercises.Add(new CoachPulse.Domain.Entities.ProgramExercise { Id = Guid.NewGuid(), ProgramId = template.Id, ExerciseId = exercise.ExerciseId, Sets = exercise.Sets, Reps = exercise.Reps, RestSeconds = exercise.RestSeconds, Day = exercise.Day, SortOrder = exercise.SortOrder });
        await _context.SaveChangesAsync();
        return Created($"/api/programs/{template.Id}", new { template.Id, template.Title, template.IsTemplate, template.DurationDays, template.RowVersion });
    }

    private async Task<IActionResult> CreateProgramTemplateCore(CreateProgramRequest request)
    {
        var userIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = User.FindFirst("tenantId")?.Value;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Template title is required."
            });
        }

        if (role != UserRole.TenantOwner.ToString() &&
            role != UserRole.CoachStaff.ToString())
        {
            return Forbid();
        }

        var template = new CoachPulse.Domain.Entities.Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = null,
            CoachId = userId,
            Title = request.Title.Trim(),
            IsTemplate = true
        };

        _context.Programs.Add(template);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Program template created successfully.",
            template.Id,
            template.Title,
            template.TenantId,
            template.CoachId,
            template.IsTemplate
        });
    }

    // GET: api/coach/programs/templates
    // List templates available to the current coach/tenant.
    [HttpGet("programs/templates")]
    [Obsolete("Use GET /api/programs/templates.")]
    public async Task<IActionResult> GetProgramTemplates()
    {
        if (Request.Path == "/api/coach/programs/templates") RouteDeprecation.AddHeaders(Response, "/api/programs/templates");
        return await GetProgramTemplatesCore();
    }

    [HttpGet("~/api/programs/templates")]
    public Task<IActionResult> GetProgramTemplatesCanonical() => GetProgramTemplatesCore();

    private async Task<IActionResult> GetProgramTemplatesCore()
    {
        var userIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = User.FindFirst("tenantId")?.Value;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized();
        }

        var query = _context.Programs
            .AsNoTracking()
            .Where(p =>
                p.TenantId == tenantId &&
                p.IsTemplate &&
                p.ClientId == null);

        if (role != UserRole.CoachStaff.ToString() && role != UserRole.TenantOwner.ToString())
        {
            return Forbid();
        }

        var templates = await query
            .OrderBy(p => p.Title)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.CoachId,
                p.TenantId
            })
            .ToListAsync();

        return Ok(templates);
    }

    // POST: api/coach/programs/templates/{templateId}/create
    // Create a client program by copying a template and its exercises.
    [HttpPost("programs/templates/{templateId:guid}/create")]
    [Obsolete("Use POST /api/programs/{id}/from-template.")]
    public async Task<IActionResult> CreateProgramFromTemplate(
        Guid templateId,
        [FromBody] CreateProgramFromTemplateRequest request)
    {
        if (Request.Path.StartsWithSegments("/api/coach/programs/templates")) RouteDeprecation.AddHeaders(Response, $"/api/programs/{templateId}/from-template");
        return await CreateProgramFromTemplateCore(templateId, request);
    }

    [HttpPost("~/api/programs/{templateId:guid}/from-template")]
    public Task<IActionResult> CreateProgramFromTemplateCanonical(Guid templateId, [FromBody] CreateProgramFromTemplateRequest request) => CreateProgramFromTemplateCore(templateId, request);

    private async Task<IActionResult> CreateProgramFromTemplateCore(Guid templateId, CreateProgramFromTemplateRequest request)
    {
        var userIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = User.FindFirst("tenantId")?.Value;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized();
        }

        if (request.ClientId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "ClientId is required."
            });
        }

        if (role != UserRole.TenantOwner.ToString() &&
            role != UserRole.CoachStaff.ToString())
        {
            return Forbid();
        }

        var template = await _context.Programs
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == templateId &&
                p.TenantId == tenantId &&
                p.IsTemplate &&
                p.ClientId == null);

        if (template is null)
        {
            return NotFound(new
            {
                message = "Template not found."
            });
        }

        var client = await _context.Clients
            .FirstOrDefaultAsync(c =>
                c.Id == request.ClientId &&
                c.TenantId == tenantId);

        if (client is null)
        {
            return NotFound(new
            {
                message = "Client not found."
            });
        }

        // Staff can only create programs for their own clients.
        if (role == UserRole.CoachStaff.ToString() &&
            client.CoachId != userId)
        {
            return NotFound();
        }

        var templateExercises = await _context.ProgramExercises
            .AsNoTracking()
            .Where(pe => pe.ProgramId == template.Id)
            .ToListAsync();

        var newProgram = new CoachPulse.Domain.Entities.Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client.Id,
            CoachId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? template.Title
                : request.Title.Trim(),
            IsTemplate = false
        };

        _context.Programs.Add(newProgram);

        foreach (var item in templateExercises)
        {
            _context.ProgramExercises.Add(
                new CoachPulse.Domain.Entities.ProgramExercise
                {
                    Id = Guid.NewGuid(),
                    ProgramId = newProgram.Id,
                    ExerciseId = item.ExerciseId,
                    Sets = item.Sets,
                    Reps = item.Reps,
                    RestSeconds = item.RestSeconds,
                    Day = item.Day,
                    SortOrder = item.SortOrder
                });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Program created from template successfully.",
            Program = new
            {
                newProgram.Id,
                newProgram.Title,
                newProgram.ClientId,
                newProgram.CoachId,
                newProgram.TenantId,
                newProgram.IsTemplate
            },
            ExercisesCopied = templateExercises.Count
        });
    }
}
