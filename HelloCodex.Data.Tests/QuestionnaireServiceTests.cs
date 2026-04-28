using HelloCodex.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelloCodex.Data.Tests;

public sealed class QuestionnaireServiceTests
{
    [Fact]
    public async Task UpdateQuestionnaireAsyncRejectsChangesAfterSubmission()
    {
        await using var fixture = new DataContextFixture();
        var service = new QuestionnaireService(fixture.Context);
        var created = await service.CreateQuestionnaireAsync(CreateDefinition());

        Assert.Equal(QuestionnaireOperationStatus.Success, created.Status);
        var questionnaireId = created.Value!.Id;

        var submission = await service.SubmitAnswersAsync(
            questionnaireId,
            new SubmissionInput(
                [
                    new AnswerInput(created.Value.Questions[0].Id, null, true, null),
                    new AnswerInput(created.Value.Questions[1].Id, null, null, 4),
                ]));

        Assert.Equal(QuestionnaireOperationStatus.Success, submission.Status);

        var update = await service.UpdateQuestionnaireAsync(
            questionnaireId,
            CreateDefinition("Geänderter Titel"));

        Assert.Equal(QuestionnaireOperationStatus.Conflict, update.Status);
    }

    [Fact]
    public async Task UpdateQuestionnaireAsyncRejectsRemovedQuestions()
    {
        await using var fixture = new DataContextFixture();
        var service = new QuestionnaireService(fixture.Context);
        var created = await service.CreateQuestionnaireAsync(CreateDefinition());

        Assert.Equal(QuestionnaireOperationStatus.Success, created.Status);

        var input = new QuestionnaireDefinitionInput(
            "Kundenzufriedenheit",
            "Quartalsumfrage",
            "kunden",
            [new QuestionInput(created.Value!.Questions[0].Id, "Würden Sie uns empfehlen?", QuestionAnswerType.YesNo, true)]);

        var result = await service.UpdateQuestionnaireAsync(created.Value.Id, input);

        Assert.Equal(QuestionnaireOperationStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Issues, issue => issue.Field == "questions");
    }

    [Fact]
    public async Task GetResultsAsyncReturnsAggregatedAnswers()
    {
        await using var fixture = new DataContextFixture();
        var service = new QuestionnaireService(fixture.Context);
        var created = await service.CreateQuestionnaireAsync(CreateDefinition());

        Assert.Equal(QuestionnaireOperationStatus.Success, created.Status);

        await service.SubmitAnswersAsync(
            created.Value!.Id,
            new SubmissionInput(
                [
                    new AnswerInput(created.Value.Questions[0].Id, null, true, null),
                    new AnswerInput(created.Value.Questions[1].Id, null, null, 4),
                ]));
        await service.SubmitAnswersAsync(
            created.Value.Id,
            new SubmissionInput(
                [
                    new AnswerInput(created.Value.Questions[0].Id, null, false, null),
                    new AnswerInput(created.Value.Questions[1].Id, null, null, 2),
                ]));

        var results = await service.GetResultsAsync(created.Value.Id);

        Assert.NotNull(results);
        var yesNo = results.Questions[0].YesNo;
        var likert = results.Questions[1].Likert;
        Assert.NotNull(yesNo);
        Assert.Equal(1, yesNo.YesCount);
        Assert.Equal(1, yesNo.NoCount);
        Assert.Equal(50, yesNo.YesPercentage);
        Assert.NotNull(likert);
        Assert.Equal(3, likert.Average);
        Assert.Equal(50, likert.Distribution.Single(item => item.Value == 2).Percentage);
        Assert.Equal(50, likert.Distribution.Single(item => item.Value == 4).Percentage);
    }

    private static QuestionnaireDefinitionInput CreateDefinition(string title = "Kundenzufriedenheit") =>
        new(
            title,
            "Quartalsumfrage",
            "kunden",
            [
                new QuestionInput(null, "Würden Sie uns empfehlen?", QuestionAnswerType.YesNo, true),
                new QuestionInput(null, "Wie bewerten Sie uns?", QuestionAnswerType.Likert1To5, true),
            ]);

    private sealed class DataContextFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        public DataContextFixture()
        {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(connection)
                .Options;
            Context = new DataContext(options);
            Context.Database.EnsureCreated();
        }

        public DataContext Context { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
