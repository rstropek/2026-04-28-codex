using Microsoft.EntityFrameworkCore;

namespace HelloCodex.Data;

public sealed class QuestionnaireService(DataContext dbContext)
{
    public async Task<IReadOnlyList<QuestionnaireSummaryDto>> ListQuestionnairesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Questionnaires
            .AsNoTracking()
            .OrderBy(questionnaire => questionnaire.Code)
            .Select(questionnaire => new QuestionnaireSummaryDto(
                questionnaire.Id,
                questionnaire.Code,
                questionnaire.Description,
                questionnaire.Tags,
                questionnaire.Submissions.Any(),
                questionnaire.Questions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<QuestionnaireDetailsDto?> GetQuestionnaireAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var questionnaire = await dbContext.Questionnaires
            .AsNoTracking()
            .Include(item => item.Questions)
            .Include(item => item.Submissions)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return questionnaire is null ? null : MapQuestionnaireDetails(questionnaire);
    }

    public async Task<QuestionnaireOperationResult<QuestionnaireDetailsDto>> CreateQuestionnaireAsync(
        QuestionnaireDefinitionInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = QuestionnaireValidation.ValidateDefinition(input);
        if (!validation.IsValid)
        {
            return QuestionnaireOperationResult.ValidationFailed<QuestionnaireDetailsDto>(validation.Issues);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var questionnaire = new Questionnaire
        {
            Code = input.Title.Trim(),
            Description = input.Description.Trim(),
            Tags = QuestionnaireValidation.NormalizeTags(input.Tags),
        };

        for (var index = 0; index < input.Questions.Count; index++)
        {
            var question = input.Questions[index];
            questionnaire.Questions.Add(new Question
            {
                SortOrder = index,
                Text = question.Text.Trim(),
                AnswerType = question.AnswerType,
                IsRequired = question.IsRequired,
            });
        }

        dbContext.Questionnaires.Add(questionnaire);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return QuestionnaireOperationResult.Success<QuestionnaireDetailsDto>(MapQuestionnaireDetails(questionnaire));
    }

    public async Task<QuestionnaireOperationResult<QuestionnaireDetailsDto>> UpdateQuestionnaireAsync(
        int id,
        QuestionnaireDefinitionInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = QuestionnaireValidation.ValidateDefinition(input);
        if (!validation.IsValid)
        {
            return QuestionnaireOperationResult.ValidationFailed<QuestionnaireDetailsDto>(validation.Issues);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var questionnaire = await dbContext.Questionnaires
            .Include(item => item.Questions)
            .Include(item => item.Submissions)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (questionnaire is null)
        {
            return QuestionnaireOperationResult.NotFound<QuestionnaireDetailsDto>("Der Fragebogen wurde nicht gefunden.");
        }

        if (questionnaire.Submissions.Count > 0)
        {
            return QuestionnaireOperationResult.Conflict<QuestionnaireDetailsDto>(
                "Dieser Fragebogen hat bereits Antworten und kann nicht mehr geändert werden.");
        }

        var existingQuestionsById = questionnaire.Questions.ToDictionary(question => question.Id);
        var requestedExistingIds = input.Questions
            .Where(question => question.Id is > 0)
            .Select(question => question.Id.GetValueOrDefault())
            .ToArray();

        if (requestedExistingIds.Length != requestedExistingIds.Distinct().Count())
        {
            return QuestionnaireOperationResult.ValidationFailed<QuestionnaireDetailsDto>(
                [new ValidationIssue("questions", "Eine bestehende Frage darf im Update nur einmal vorkommen.")]);
        }

        if (requestedExistingIds.Any(questionId => !existingQuestionsById.ContainsKey(questionId)))
        {
            return QuestionnaireOperationResult.ValidationFailed<QuestionnaireDetailsDto>(
                [new ValidationIssue("questions", "Eine Frage gehört nicht zu diesem Fragebogen.")]);
        }

        if (existingQuestionsById.Keys.Except(requestedExistingIds).Any())
        {
            return QuestionnaireOperationResult.ValidationFailed<QuestionnaireDetailsDto>(
                [new ValidationIssue("questions", "Bestehende Fragen dürfen nicht gelöscht werden.")]);
        }

        questionnaire.Code = input.Title.Trim();
        questionnaire.Description = input.Description.Trim();
        questionnaire.Tags = QuestionnaireValidation.NormalizeTags(input.Tags);

        for (var index = 0; index < input.Questions.Count; index++)
        {
            var questionInput = input.Questions[index];
            if (questionInput.Id is > 0 && existingQuestionsById.TryGetValue(questionInput.Id.Value, out var question))
            {
                question.SortOrder = index;
                question.Text = questionInput.Text.Trim();
                question.AnswerType = questionInput.AnswerType;
                question.IsRequired = questionInput.IsRequired;
            }
            else
            {
                questionnaire.Questions.Add(new Question
                {
                    SortOrder = index,
                    Text = questionInput.Text.Trim(),
                    AnswerType = questionInput.AnswerType,
                    IsRequired = questionInput.IsRequired,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return QuestionnaireOperationResult.Success<QuestionnaireDetailsDto>(MapQuestionnaireDetails(questionnaire));
    }

    public async Task<QuestionnaireOperationResult<SubmissionCreatedDto>> SubmitAnswersAsync(
        int questionnaireId,
        SubmissionInput input,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var questionnaire = await dbContext.Questionnaires
            .Include(item => item.Questions)
            .SingleOrDefaultAsync(item => item.Id == questionnaireId, cancellationToken);

        if (questionnaire is null)
        {
            return QuestionnaireOperationResult.NotFound<SubmissionCreatedDto>("Der Fragebogen wurde nicht gefunden.");
        }

        var questionDtos = questionnaire.Questions
            .OrderBy(question => question.SortOrder)
            .Select(MapQuestion)
            .ToArray();
        var validation = QuestionnaireValidation.ValidateSubmission(questionDtos, input);

        if (!validation.IsValid)
        {
            return QuestionnaireOperationResult.ValidationFailed<SubmissionCreatedDto>(validation.Issues);
        }

        var submission = new QuestionnaireSubmission
        {
            QuestionnaireId = questionnaireId,
            SubmittedAtUtc = DateTime.UtcNow,
        };

        foreach (var answer in input.Answers)
        {
            var question = questionDtos.Single(item => item.Id == answer.QuestionId);
            if (!TryCreateAnswer(question, answer, out var storedAnswer))
            {
                continue;
            }

            submission.Answers.Add(storedAnswer);
        }

        dbContext.QuestionnaireSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return QuestionnaireOperationResult.Success<SubmissionCreatedDto>(new SubmissionCreatedDto(submission.Id));
    }

    public async Task<QuestionnaireResultsDto?> GetResultsAsync(
        int questionnaireId,
        CancellationToken cancellationToken = default)
    {
        var questionnaire = await dbContext.Questionnaires
            .AsNoTracking()
            .Include(item => item.Questions)
                .ThenInclude(question => question.Answers)
            .SingleOrDefaultAsync(item => item.Id == questionnaireId, cancellationToken);

        if (questionnaire is null)
        {
            return null;
        }

        var questionResults = questionnaire.Questions
            .OrderBy(question => question.SortOrder)
            .Select(MapQuestionResult)
            .ToArray();

        return new QuestionnaireResultsDto(questionnaire.Id, questionnaire.Code, questionResults);
    }

    private static QuestionnaireDetailsDto MapQuestionnaireDetails(Questionnaire questionnaire)
    {
        var questions = questionnaire.Questions
            .OrderBy(question => question.SortOrder)
            .Select(MapQuestion)
            .ToArray();

        return new QuestionnaireDetailsDto(
            questionnaire.Id,
            questionnaire.Code,
            questionnaire.Description,
            questionnaire.Tags,
            questionnaire.Submissions.Count > 0,
            questions);
    }

    private static QuestionDto MapQuestion(Question question) =>
        new(question.Id, question.Text, question.AnswerType, question.IsRequired, question.SortOrder);

    private static bool TryCreateAnswer(QuestionDto question, AnswerInput input, out QuestionAnswer answer)
    {
        answer = new QuestionAnswer { QuestionId = input.QuestionId };

        switch (question.AnswerType)
        {
            case QuestionAnswerType.Text:
                if (string.IsNullOrWhiteSpace(input.TextValue))
                {
                    return false;
                }

                answer.TextValue = input.TextValue.Trim();
                return true;
            case QuestionAnswerType.YesNo:
                if (!input.BoolValue.HasValue)
                {
                    return false;
                }

                answer.BoolValue = input.BoolValue.Value;
                return true;
            case QuestionAnswerType.Likert1To5:
                if (!input.NumberValue.HasValue)
                {
                    return false;
                }

                answer.NumberValue = input.NumberValue.Value;
                return true;
            default:
                return false;
        }
    }

    private static QuestionResultDto MapQuestionResult(Question question) =>
        question.AnswerType switch
        {
            QuestionAnswerType.Text => new QuestionResultDto(
                question.Id,
                question.Text,
                question.AnswerType,
                question.Answers
                    .Where(answer => !string.IsNullOrWhiteSpace(answer.TextValue))
                    .Select(answer => answer.TextValue!)
                    .ToArray(),
                null,
                null),
            QuestionAnswerType.YesNo => new QuestionResultDto(
                question.Id,
                question.Text,
                question.AnswerType,
                [],
                BuildYesNoResult(question.Answers),
                null),
            QuestionAnswerType.Likert1To5 => new QuestionResultDto(
                question.Id,
                question.Text,
                question.AnswerType,
                [],
                null,
                BuildLikertResult(question.Answers)),
            _ => new QuestionResultDto(question.Id, question.Text, question.AnswerType, [], null, null),
        };

    private static YesNoResultDto BuildYesNoResult(IReadOnlyCollection<QuestionAnswer> answers)
    {
        var yesCount = answers.Count(answer => answer.BoolValue == true);
        var noCount = answers.Count(answer => answer.BoolValue == false);
        var total = yesCount + noCount;

        return new YesNoResultDto(
            yesCount,
            noCount,
            CalculatePercentage(yesCount, total),
            CalculatePercentage(noCount, total));
    }

    private static LikertResultDto BuildLikertResult(IReadOnlyCollection<QuestionAnswer> answers)
    {
        var values = answers
            .Where(answer => answer.NumberValue.HasValue)
            .Select(answer => answer.NumberValue.GetValueOrDefault())
            .ToArray();
        var distribution = Enumerable.Range(1, 5)
            .Select(value =>
            {
                var count = values.Count(item => item == value);
                return new ValueDistributionDto(value, count, CalculatePercentage(count, values.Length));
            })
            .ToArray();
        var average = values.Length == 0 ? null : (double?)Math.Round(values.Average(), 2);

        return new LikertResultDto(average, distribution);
    }

    private static double CalculatePercentage(int count, int total) =>
        total == 0 ? 0 : Math.Round(count * 100.0 / total, 1);
}
