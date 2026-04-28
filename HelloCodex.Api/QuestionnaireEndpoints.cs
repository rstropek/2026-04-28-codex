using HelloCodex.Data;
using Microsoft.AspNetCore.Routing;

namespace HelloCodex.Api;

public static class QuestionnaireEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapQuestionnaireEndpoints()
        {
            var questionnaires = endpoints.MapGroup("/api/questionnaires").WithTags("Questionnaires");

            questionnaires.MapGet("/", async (QuestionnaireService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.ListQuestionnairesAsync(cancellationToken)));

            questionnaires.MapGet("/{id:int}", GetQuestionnaireAsync);
            questionnaires.MapPost("/", CreateQuestionnaireAsync);
            questionnaires.MapPut("/{id:int}", UpdateQuestionnaireAsync);
            questionnaires.MapPost("/{id:int}/submissions", SubmitAnswersAsync);
            questionnaires.MapGet("/{id:int}/results", GetResultsAsync);

            return endpoints;
        }
    }

    private static async Task<IResult> GetQuestionnaireAsync(
        int id,
        QuestionnaireService service,
        CancellationToken cancellationToken)
    {
        var questionnaire = await service.GetQuestionnaireAsync(id, cancellationToken);
        return questionnaire is null ? TypedResults.NotFound() : TypedResults.Ok(questionnaire);
    }

    private static async Task<IResult> CreateQuestionnaireAsync(
        QuestionnaireDefinitionInput input,
        QuestionnaireService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateQuestionnaireAsync(input, cancellationToken);
        return ToQuestionnaireResult(result);
    }

    private static async Task<IResult> UpdateQuestionnaireAsync(
        int id,
        QuestionnaireDefinitionInput input,
        QuestionnaireService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateQuestionnaireAsync(id, input, cancellationToken);
        return ToQuestionnaireResult(result);
    }

    private static async Task<IResult> SubmitAnswersAsync(
        int id,
        SubmissionInput input,
        QuestionnaireService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SubmitAnswersAsync(id, input, cancellationToken);
        return ToSubmissionResult(result);
    }

    private static async Task<IResult> GetResultsAsync(
        int id,
        QuestionnaireService service,
        CancellationToken cancellationToken)
    {
        var results = await service.GetResultsAsync(id, cancellationToken);
        return results is null ? TypedResults.NotFound() : TypedResults.Ok(results);
    }

    private static IResult ToQuestionnaireResult(
        QuestionnaireOperationResult<QuestionnaireDetailsDto> result) =>
        result.Status switch
        {
            QuestionnaireOperationStatus.Success => TypedResults.Ok(result.Value),
            QuestionnaireOperationStatus.NotFound => TypedResults.NotFound(),
            QuestionnaireOperationStatus.ValidationFailed => TypedResults.ValidationProblem(
                ToValidationDictionary(result.Issues)),
            QuestionnaireOperationStatus.Conflict => TypedResults.Problem(
                result.Message,
                statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Problem("Der Vorgang konnte nicht verarbeitet werden."),
        };

    private static IResult ToSubmissionResult(
        QuestionnaireOperationResult<SubmissionCreatedDto> result) =>
        result.Status switch
        {
            QuestionnaireOperationStatus.Success => TypedResults.Ok(result.Value),
            QuestionnaireOperationStatus.NotFound => TypedResults.NotFound(),
            QuestionnaireOperationStatus.ValidationFailed => TypedResults.ValidationProblem(
                ToValidationDictionary(result.Issues)),
            QuestionnaireOperationStatus.Conflict => TypedResults.Problem(
                result.Message,
                statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Problem("Der Vorgang konnte nicht verarbeitet werden."),
        };

    private static Dictionary<string, string[]> ToValidationDictionary(IReadOnlyList<ValidationIssue> issues) =>
        issues.GroupBy(issue => issue.Field).ToDictionary(
            group => group.Key,
            group => group.Select(issue => issue.Message).ToArray());
}
