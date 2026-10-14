using System.Text.Json;

namespace CoachPulse.Application.DTOs;

public sealed record AiExerciseOption(Guid Id, string Name, string MuscleGroup, string? Equipment);
public sealed record AiGenerateInput(Guid ClientId, string Goal, string Level, int DaysPerWeek, int SessionMinutes, int DurationWeeks, IReadOnlyList<string> Equipment, IReadOnlyList<string> Injuries, IReadOnlyList<string> DietaryRestrictions, int? CaloriesTarget, string Language, string ExtraNotes, IReadOnlyList<AiExerciseOption> AvailableExercises);
public sealed record AiRegenerateInput(AiGenerateInput Program, Guid OriginalProgramId, string CoachNotes);
public sealed record AiWorkoutContent(JsonElement WorkoutPlan, JsonElement MealPlan, IReadOnlyList<string> UnmatchedExercises);
public sealed record AiChurnFeatures(int CheckInsLast14d, double CheckInRateLast30d, double WorkoutCompletionRate14d, double AvgReplyTimeHours, int DaysSinceLastLogin, int DaysToSubscriptionEnd, int MessagesSentLast14d, int SubscriptionsRenewedCount, bool HasPendingPayment);
public sealed record AiChurnRequest(Guid ClientId, AiChurnFeatures Features);
public sealed record AiChurnFactor(string Feature, double Impact);
public sealed record AiChurnResponse(Guid ClientId, double RiskScore, string RiskLevel, IReadOnlyList<AiChurnFactor> TopFactors, string ModelVersion);
public sealed record AiChurnBatchResponse(IReadOnlyList<AiChurnResponse> Items);
public sealed record AiModelInfo(string ModelVersion, double Accuracy, double Precision, double Recall, string TrainingData);

public sealed record AiGeneratedProgramEdit(string RowVersion, JsonElement WorkoutPlan, JsonElement MealPlan, IReadOnlyList<string> UnmatchedExercises);

public sealed record AiGenerateApiRequest(Guid ClientId, string Goal, string Level, int DaysPerWeek, int SessionMinutes, int DurationWeeks, List<string> Equipment, List<string> Injuries, List<string> DietaryRestrictions, int? CaloriesTarget, string Language, string ExtraNotes);
public sealed record AiRegenerateApiRequest(string CoachNotes);
