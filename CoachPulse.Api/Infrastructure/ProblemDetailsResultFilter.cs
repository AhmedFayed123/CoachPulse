using CoachPulse.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections;
using System.Reflection;

namespace CoachPulse.Api.Infrastructure;

/// <summary>Normalizes controller error results to RFC 7807 problem details.</summary>
public sealed class ProblemDetailsResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var status = context.Result switch
        {
            ObjectResult result => result.StatusCode ?? context.HttpContext.Response.StatusCode,
            StatusCodeResult result => result.StatusCode,
            ContentResult result => result.StatusCode ?? context.HttpContext.Response.StatusCode,
            ForbidResult => StatusCodes.Status403Forbidden,
            _ => context.HttpContext.Response.StatusCode
        };

        if (status is >= 400 and <= 599)
        {
            var original = context.Result switch
            {
                ObjectResult result => result.Value,
                ContentResult result => result.Content,
                _ => null
            };

            var problem = original as ProblemDetails ?? new ProblemDetails
            {
                Status = status,
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = original as string ?? GetDetail(original)
            };
            problem.Status = status;
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            var objectResult = new ObjectResult(problem) { StatusCode = status };
            objectResult.ContentTypes.Add("application/problem+json");
            context.Result = objectResult;
        }
        else if (context.Result is ObjectResult { Value: { } value } response && TryGetItems(value, out var items, out var existingPage, out var existingPageSize, out var existingTotal))
        {
            var query = context.HttpContext.Request.Query;
            var page = ReadInt(query["page"], existingPage, 1);
            var pageSize = ReadInt(query["pageSize"], existingPageSize, 20);
            if (page < 1 || pageSize is < 1 or > 100)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Invalid pagination",
                    Detail = "page must be at least 1 and pageSize must be between 1 and 100.",
                    Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
                }) { StatusCode = StatusCodes.Status422UnprocessableEntity };
                await next();
                return;
            }
            var total = existingTotal ?? items.Count;
            var selected = existingPage.HasValue
                ? items
                : items.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            response.Value = PagedResult<object?>.Create(selected, page, pageSize, total);
        }

        await next();
    }

    private static bool TryGetItems(object value, out IReadOnlyList<object?> items, out int? page, out int? pageSize, out int? total)
    {
        page = pageSize = total = null;
        if (value is IEnumerable sequence && value is not string && value is not IDictionary)
        {
            items = sequence.Cast<object?>().ToArray();
            return true;
        }

        var type = value.GetType();
        var itemProperty = Find(type, "items");
        if (itemProperty?.GetValue(value) is not IEnumerable rows || itemProperty.GetValue(value) is string)
        {
            items = [];
            return false;
        }
        items = rows.Cast<object?>().ToArray();
        page = ReadProperty(value, "page");
        pageSize = ReadProperty(value, "pageSize") ?? ReadProperty(value, "limit");
        total = ReadProperty(value, "totalCount") ?? ReadProperty(value, "total");
        return true;
    }

    private static PropertyInfo? Find(Type type, string name) => type.GetProperties().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    private static string? GetDetail(object? value)
    {
        if (value is null) return null;
        var detail = Find(value.GetType(), "detail")?.GetValue(value) ?? Find(value.GetType(), "message")?.GetValue(value) ?? Find(value.GetType(), "title")?.GetValue(value);
        return detail?.ToString();
    }
    private static int? ReadProperty(object value, string name) => Find(value.GetType(), name)?.GetValue(value) as int?;
    private static int ReadInt(string? raw, int? current, int fallback) => int.TryParse(raw, out var parsed) ? parsed : current ?? fallback;
}
