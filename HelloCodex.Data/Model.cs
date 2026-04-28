using System.ComponentModel.DataAnnotations;

namespace HelloCodex.Data;

public enum QuestionAnswerType
{
    Text = 1,
    YesNo = 2,
    Likert1To5 = 3,
}

public sealed class Questionnaire
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty;

    public List<Question> Questions { get; } = [];

    public List<QuestionnaireSubmission> Submissions { get; } = [];
}

public sealed class Question
{
    public int Id { get; set; }

    public int QuestionnaireId { get; set; }

    public Questionnaire? Questionnaire { get; set; }

    public int SortOrder { get; set; }

    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public QuestionAnswerType AnswerType { get; set; }

    public bool IsRequired { get; set; }

    public List<QuestionAnswer> Answers { get; } = [];
}

public sealed class QuestionnaireSubmission
{
    public int Id { get; set; }

    public int QuestionnaireId { get; set; }

    public Questionnaire? Questionnaire { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public List<QuestionAnswer> Answers { get; } = [];
}

public sealed class QuestionAnswer
{
    public int Id { get; set; }

    public int QuestionnaireSubmissionId { get; set; }

    public QuestionnaireSubmission? QuestionnaireSubmission { get; set; }

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    [MaxLength(4000)]
    public string? TextValue { get; set; }

    public bool? BoolValue { get; set; }

    public int? NumberValue { get; set; }
}
