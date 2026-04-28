using System.ComponentModel.DataAnnotations;

namespace HelloCodex.Data;

public sealed class Questionnaire
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
}
