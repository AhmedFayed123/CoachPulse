using System.ComponentModel.DataAnnotations;

namespace CoachPulse.Api.Models
{
    public sealed record CreateCheckInUpdateRequest(decimal Weight, string? Measurements);
    public sealed record CheckInFeedbackRequest(string Feedback);
    public class CreateCheckInFormRequest
    {
        [Range(typeof(decimal), "1", "500")]
        public decimal Weight { get; set; }

        [StringLength(2000)]
        public string? Measurements { get; set; }

        public IFormFile? Photo { get; set; }
        public List<Guid> PhotoFileIds { get; set; } = [];
    }
}
