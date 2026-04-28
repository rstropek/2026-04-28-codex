namespace HelloCodex.Data;

public sealed record QuestionnaireDefinitionInput(
    string Title,
    string Description,
    string Tags,
    IReadOnlyList<QuestionInput> Questions);

public sealed record QuestionInput(
    int? Id,
    string Text,
    QuestionAnswerType AnswerType,
    bool IsRequired);

public sealed record SubmissionInput(IReadOnlyList<AnswerInput> Answers);

public sealed record AnswerInput(
    int QuestionId,
    string? TextValue,
    bool? BoolValue,
    int? NumberValue);

public sealed record QuestionnaireSummaryDto(
    int Id,
    string Title,
    string Description,
    string Tags,
    bool HasSubmissions,
    int QuestionCount);

public sealed record QuestionnaireDetailsDto(
    int Id,
    string Title,
    string Description,
    string Tags,
    bool HasSubmissions,
    IReadOnlyList<QuestionDto> Questions);

public sealed record QuestionDto(
    int Id,
    string Text,
    QuestionAnswerType AnswerType,
    bool IsRequired,
    int SortOrder);

public sealed record SubmissionCreatedDto(int Id);

public sealed record QuestionnaireResultsDto(
    int Id,
    string Title,
    IReadOnlyList<QuestionResultDto> Questions);

public sealed record QuestionResultDto(
    int QuestionId,
    string Text,
    QuestionAnswerType AnswerType,
    IReadOnlyList<string> TextAnswers,
    YesNoResultDto? YesNo,
    LikertResultDto? Likert);

public sealed record YesNoResultDto(
    int YesCount,
    int NoCount,
    double YesPercentage,
    double NoPercentage);

public sealed record LikertResultDto(
    double? Average,
    IReadOnlyList<ValueDistributionDto> Distribution);

public sealed record ValueDistributionDto(
    int Value,
    int Count,
    double Percentage);

public sealed record ValidationIssue(string Field, string Message);

public sealed record QuestionnaireValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;

    public static QuestionnaireValidationResult Valid { get; } = new([]);
}

public enum QuestionnaireOperationStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict,
}

public sealed record QuestionnaireOperationResult<T>(
    QuestionnaireOperationStatus Status,
    T? Value,
    IReadOnlyList<ValidationIssue> Issues,
    string? Message);

public static class QuestionnaireOperationResult
{
    public static QuestionnaireOperationResult<T> Success<T>(T value) =>
        new(QuestionnaireOperationStatus.Success, value, [], null);

    public static QuestionnaireOperationResult<T> NotFound<T>(string message) =>
        new(QuestionnaireOperationStatus.NotFound, default, [], message);

    public static QuestionnaireOperationResult<T> ValidationFailed<T>(IReadOnlyList<ValidationIssue> issues) =>
        new(QuestionnaireOperationStatus.ValidationFailed, default, issues, null);

    public static QuestionnaireOperationResult<T> Conflict<T>(string message) =>
        new(QuestionnaireOperationStatus.Conflict, default, [], message);
}
