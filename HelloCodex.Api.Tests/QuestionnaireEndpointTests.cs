using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HelloCodex.Data;

namespace HelloCodex.Api.Tests;

[Collection(ApiTestScope.Name)]
public sealed class QuestionnaireEndpointTests(ApiFactoryFixture factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task CreateQuestionnaireReturnsPersistedQuestionnaire()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/questionnaires", CreateDefinition(), JsonOptions);

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<QuestionnaireDetailsDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Kundenzufriedenheit", created.Title);
        Assert.Equal(2, created.Questions.Count);
    }

    [Fact]
    public async Task CreateQuestionnaireReturnsBadRequestForInvalidInput()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        var input = new QuestionnaireDefinitionInput(string.Empty, string.Empty, string.Empty, []);

        var response = await client.PostAsJsonAsync("/api/questionnaires", input, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuestionnaireReturnsConflictAfterSubmission()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/questionnaires", CreateDefinition(), JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<QuestionnaireDetailsDto>(JsonOptions);

        Assert.NotNull(created);

        var submission = new SubmissionInput(
            [
                new AnswerInput(created.Questions[0].Id, null, true, null),
                new AnswerInput(created.Questions[1].Id, null, null, 5),
            ]);
        var submitResponse = await client.PostAsJsonAsync(
            $"/api/questionnaires/{created.Id}/submissions",
            submission,
            JsonOptions);
        submitResponse.EnsureSuccessStatusCode();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/questionnaires/{created.Id}",
            CreateDefinition("Neuer Titel"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }

    private static QuestionnaireDefinitionInput CreateDefinition(string title = "Kundenzufriedenheit") =>
        new(
            title,
            "Quartalsumfrage",
            "kunden, quartal",
            [
                new QuestionInput(null, "Würden Sie uns empfehlen?", QuestionAnswerType.YesNo, true),
                new QuestionInput(null, "Wie bewerten Sie uns?", QuestionAnswerType.Likert1To5, true),
            ]);
}
