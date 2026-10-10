
using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.DTOs.Coach;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
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

    public CoachController(CoachPulseDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost("clients/invite")]
    public async Task<IActionResult> InviteClient(
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
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _context.ClientInvitations.Add(invitation);

        await _context.SaveChangesAsync();

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
    public async Task<IActionResult> GetClients()
    {
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

        var query = _context.Clients
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (roleClaim.Value == UserRole.Staff.ToString())
        {
            query = query.Where(x => x.CoachId == userId);
        }
        else if (roleClaim.Value != UserRole.Owner.ToString())
        {
            return Forbid();
        }

        var clients = await query
            .OrderBy(x => x.User!.Email)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                Email = x.User!.Email,
                x.CoachId,
                x.Goals,
                x.HealthInfo
            })
            .ToListAsync();

        return Ok(clients);
    }
    [HttpPost("programs")]
    public async Task<IActionResult> CreateProgram(
    CreateProgramRequest request)
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

        if (request.ClientId == Guid.Empty)
            return BadRequest("ClientId is required.");

        var client = await _context.Clients
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClientId &&
                x.TenantId == tenantId);

        if (client == null)
            return NotFound("Client not found.");

        if (roleClaim.Value == UserRole.Staff.ToString())
        {
            if (client.CoachId != userId)
                return Forbid();
        }
        else if (roleClaim.Value != UserRole.Owner.ToString())
        {
            return Forbid();
        }

        var program = new CoachPulse.Domain.Entities.Program
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client.Id,
            CoachId = userId,
            Title = request.Title.Trim(),
            IsTemplate = request.IsTemplate
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
                program.IsTemplate
            }
        });
    }
    [HttpGet("programs")]
    public async Task<IActionResult> GetPrograms()
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

        if (roleClaim.Value == UserRole.Staff.ToString())
        {
            query = query.Where(x => x.CoachId == userId);
        }
        else if (roleClaim.Value != UserRole.Owner.ToString())
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
                x.CoachId,
                x.IsTemplate,
                x.TenantId
            })
            .ToListAsync();

        return Ok(programs);
    }

    [HttpGet("exercises")]
    public async Task<IActionResult> GetExercises()
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
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> CreateExercise(
    [FromBody] CreateExerciseRequest request)
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

        var exercise = new CoachPulse.Domain.Entities.Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            MuscleGroup = muscleGroup,
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
    public async Task<IActionResult> AddExerciseToProgram(
        Guid programId,
        [FromBody] AddProgramExerciseRequest request)
    {
        if (request.ExerciseId == Guid.Empty ||
            request.Sets <= 0 ||
            request.Reps <= 0 ||
            request.Day <= 0)
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

        // Staff can manage only their own programs.
        if (role == "Staff" && program.CoachId != userId)
        {
            return Forbid();
        }

        // Shared exercise library: exercises have no TenantId.
        var exerciseExists = await _context.Exercises
            .AnyAsync(e => e.Id == request.ExerciseId);

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
            Day = request.Day
        };

        _context.ProgramExercises.Add(programExercise);

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
    public async Task<IActionResult> GetProgramExercises(Guid programId)
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

        if (role == "Staff" && program.CoachId != userId)
        {
            return Forbid();
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
                pe.Day
            })
            .OrderBy(pe => pe.Day)
            .ToListAsync();

        return Ok(exercises);
    }

    // POST: api/coach/programs/templates
    // Create a reusable program template.
    [HttpPost("programs/templates")]
    public async Task<IActionResult> CreateProgramTemplate(
        [FromBody] CreateProgramRequest request)
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

        if (role != UserRole.Owner.ToString() &&
            role != UserRole.Staff.ToString())
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
    public async Task<IActionResult> GetProgramTemplates()
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

        if (role == UserRole.Staff.ToString())
        {
            query = query.Where(p => p.CoachId == userId);
        }
        else if (role != UserRole.Owner.ToString())
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
    public async Task<IActionResult> CreateProgramFromTemplate(
        Guid templateId,
        [FromBody] CreateProgramFromTemplateRequest request)
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

        if (role != UserRole.Owner.ToString() &&
            role != UserRole.Staff.ToString())
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

        // Staff can only use their own templates.
        if (role == UserRole.Staff.ToString() &&
            template.CoachId != userId)
        {
            return Forbid();
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
        if (role == UserRole.Staff.ToString() &&
            client.CoachId != userId)
        {
            return Forbid();
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
                    Day = item.Day
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