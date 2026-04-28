using HelloCodex.Data;

namespace HelloCodex.Data.Tests;

public sealed class QuestionnaireValidationTests
{
    [Fact]
    public void ValidateDefinitionAcceptsValidQuestionnaire()
    {
        var input = new QuestionnaireDefinitionInput(
            "Kundenzufriedenheit",
            "Quartalsumfrage",
            " kunden, Q1, Kunden ",
            [new QuestionInput(null, "Wie zufrieden sind Sie?", QuestionAnswerType.Likert1To5, true)]);

        var result = QuestionnaireValidation.ValidateDefinition(input);

        Assert.True(result.IsValid);
        Assert.Equal("kunden, Q1", QuestionnaireValidation.NormalizeTags(input.Tags));
    }

    [Fact]
    public void ValidateDefinitionRejectsInvalidQuestionnaire()
    {
        var input = new QuestionnaireDefinitionInput(
            string.Empty,
            new string('x', 201),
            string.Empty,
            [new QuestionInput(null, new string('x', 501), (QuestionAnswerType)999, true)]);

        var result = QuestionnaireValidation.ValidateDefinition(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "title");
        Assert.Contains(result.Issues, issue => issue.Field == "description");
        Assert.Contains(result.Issues, issue => issue.Field == "questions[0].text");
        Assert.Contains(result.Issues, issue => issue.Field == "questions[0].answerType");
    }

    [Fact]
    public void ValidateDefinitionRejectsQuestionnaireWithoutQuestions()
    {
        var input = new QuestionnaireDefinitionInput("Titel", string.Empty, string.Empty, []);

        var result = QuestionnaireValidation.ValidateDefinition(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "questions");
    }

    [Fact]
    public void ValidateSubmissionAcceptsCompleteSubmission()
    {
        var questions = CreateQuestions();
        var input = new SubmissionInput(
            [
                new AnswerInput(1, "Guter Service", null, null),
                new AnswerInput(2, null, true, null),
                new AnswerInput(3, null, null, 5),
            ]);

        var result = QuestionnaireValidation.ValidateSubmission(questions, input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSubmissionRejectsMissingRequiredAnswer()
    {
        var questions = CreateQuestions();
        var input = new SubmissionInput(
            [
                new AnswerInput(1, "Guter Service", null, null),
                new AnswerInput(2, null, null, null),
                new AnswerInput(3, null, null, 5),
            ]);

        var result = QuestionnaireValidation.ValidateSubmission(questions, input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "question:2");
    }

    [Fact]
    public void ValidateSubmissionRejectsWrongAnswerShape()
    {
        var questions = CreateQuestions();
        var input = new SubmissionInput(
            [
                new AnswerInput(1, "Guter Service", null, null),
                new AnswerInput(2, "Ja", true, null),
                new AnswerInput(3, null, null, 6),
                new AnswerInput(99, null, true, null),
            ]);

        var result = QuestionnaireValidation.ValidateSubmission(questions, input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "question:2");
        Assert.Contains(result.Issues, issue => issue.Field == "question:3");
        Assert.Contains(result.Issues, issue => issue.Field == "answers[3].questionId");
    }

    private static IReadOnlyList<QuestionDto> CreateQuestions() =>
        [
            new QuestionDto(1, "Kommentar", QuestionAnswerType.Text, false, 0),
            new QuestionDto(2, "Empfehlung", QuestionAnswerType.YesNo, true, 1),
            new QuestionDto(3, "Bewertung", QuestionAnswerType.Likert1To5, true, 2),
        ];
}
