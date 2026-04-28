namespace HelloCodex.Data;

public static class QuestionnaireValidation
{
    public static QuestionnaireValidationResult ValidateDefinition(QuestionnaireDefinitionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var issues = new List<ValidationIssue>();
        var title = input.Title.Trim();
        var description = input.Description.Trim();
        var normalizedTags = NormalizeTags(input.Tags);

        if (title.Length == 0)
        {
            issues.Add(new ValidationIssue("title", "Der Titel ist erforderlich."));
        }

        if (title.Length > 50)
        {
            issues.Add(new ValidationIssue("title", "Der Titel darf maximal 50 Zeichen lang sein."));
        }

        if (description.Length > 200)
        {
            issues.Add(new ValidationIssue("description", "Die Beschreibung darf maximal 200 Zeichen lang sein."));
        }

        if (normalizedTags.Length > 500)
        {
            issues.Add(new ValidationIssue("tags", "Die Tags dürfen zusammen maximal 500 Zeichen lang sein."));
        }

        if (input.Questions.Count == 0)
        {
            issues.Add(new ValidationIssue("questions", "Mindestens eine Frage ist erforderlich."));
        }

        for (var index = 0; index < input.Questions.Count; index++)
        {
            var question = input.Questions[index];
            var fieldPrefix = $"questions[{index}]";
            var questionText = question.Text.Trim();

            if (questionText.Length == 0)
            {
                issues.Add(new ValidationIssue($"{fieldPrefix}.text", "Der Fragetext ist erforderlich."));
            }

            if (questionText.Length > 500)
            {
                issues.Add(new ValidationIssue($"{fieldPrefix}.text", "Der Fragetext darf maximal 500 Zeichen lang sein."));
            }

            if (!Enum.IsDefined(question.AnswerType))
            {
                issues.Add(new ValidationIssue($"{fieldPrefix}.answerType", "Der Antworttyp ist ungültig."));
            }
        }

        return issues.Count == 0 ? QuestionnaireValidationResult.Valid : new QuestionnaireValidationResult(issues);
    }

    public static QuestionnaireValidationResult ValidateSubmission(
        IReadOnlyList<QuestionDto> questions,
        SubmissionInput input)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(input);

        var issues = new List<ValidationIssue>();
        var questionsById = questions.ToDictionary(question => question.Id);
        var answersByQuestionId = new Dictionary<int, AnswerInput>();

        for (var index = 0; index < input.Answers.Count; index++)
        {
            var answer = input.Answers[index];
            var fieldPrefix = $"answers[{index}]";

            if (!questionsById.ContainsKey(answer.QuestionId))
            {
                issues.Add(new ValidationIssue($"{fieldPrefix}.questionId", "Die Frage gehört nicht zu diesem Fragebogen."));
                continue;
            }

            if (!answersByQuestionId.TryAdd(answer.QuestionId, answer))
            {
                issues.Add(new ValidationIssue($"{fieldPrefix}.questionId", "Eine Frage darf nur einmal beantwortet werden."));
            }
        }

        foreach (var question in questions.OrderBy(question => question.SortOrder))
        {
            answersByQuestionId.TryGetValue(question.Id, out var answer);
            var hasValue = answer is not null && HasAnswerValue(question.AnswerType, answer);

            if (question.IsRequired && !hasValue)
            {
                issues.Add(new ValidationIssue($"question:{question.Id}", "Diese Pflichtfrage muss beantwortet werden."));
                continue;
            }

            if (answer is not null)
            {
                ValidateAnswerShape(question, answer, issues);
            }
        }

        return issues.Count == 0 ? QuestionnaireValidationResult.Valid : new QuestionnaireValidationResult(issues);
    }

    public static string NormalizeTags(string tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var normalizedTags = tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var uniqueTags = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in normalizedTags)
        {
            if (seenTags.Add(tag))
            {
                uniqueTags.Add(tag);
            }
        }

        return string.Join(", ", uniqueTags);
    }

    private static bool HasAnswerValue(QuestionAnswerType answerType, AnswerInput answer) =>
        answerType switch
        {
            QuestionAnswerType.Text => !string.IsNullOrWhiteSpace(answer.TextValue),
            QuestionAnswerType.YesNo => answer.BoolValue.HasValue,
            QuestionAnswerType.Likert1To5 => answer.NumberValue.HasValue,
            _ => false,
        };

    private static void ValidateAnswerShape(QuestionDto question, AnswerInput answer, List<ValidationIssue> issues)
    {
        var providedValueCount = 0;
        providedValueCount += string.IsNullOrWhiteSpace(answer.TextValue) ? 0 : 1;
        providedValueCount += answer.BoolValue.HasValue ? 1 : 0;
        providedValueCount += answer.NumberValue.HasValue ? 1 : 0;

        if (providedValueCount == 0)
        {
            return;
        }

        if (providedValueCount > 1)
        {
            issues.Add(new ValidationIssue($"question:{question.Id}", "Eine Antwort darf nur einen Wert enthalten."));
            return;
        }

        switch (question.AnswerType)
        {
            case QuestionAnswerType.Text:
                if (string.IsNullOrWhiteSpace(answer.TextValue))
                {
                    issues.Add(new ValidationIssue($"question:{question.Id}", "Diese Frage erwartet eine Textantwort."));
                }

                break;
            case QuestionAnswerType.YesNo:
                if (!answer.BoolValue.HasValue)
                {
                    issues.Add(new ValidationIssue($"question:{question.Id}", "Diese Frage erwartet eine Ja/Nein-Antwort."));
                }

                break;
            case QuestionAnswerType.Likert1To5:
                if (answer.NumberValue is null)
                {
                    issues.Add(new ValidationIssue($"question:{question.Id}", "Diese Frage erwartet einen Wert von 1 bis 5."));
                }
                else if (answer.NumberValue is < 1 or > 5)
                {
                    issues.Add(new ValidationIssue($"question:{question.Id}", "Diese Frage erwartet einen Wert von 1 bis 5."));
                }

                break;
            default:
                issues.Add(new ValidationIssue($"question:{question.Id}", "Der Antworttyp ist ungültig."));
                break;
        }
    }
}
