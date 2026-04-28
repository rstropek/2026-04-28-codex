using Microsoft.EntityFrameworkCore;

namespace HelloCodex.Data;

public sealed class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Questionnaire> Questionnaires { get; set; } = null!;

    public DbSet<Question> Questions { get; set; } = null!;

    public DbSet<QuestionnaireSubmission> QuestionnaireSubmissions { get; set; } = null!;

    public DbSet<QuestionAnswer> QuestionAnswers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Questionnaire>(entity =>
        {
            entity.HasKey(questionnaire => questionnaire.Id);
            entity.Property(questionnaire => questionnaire.Code).HasMaxLength(50);
            entity.Property(questionnaire => questionnaire.Description).HasMaxLength(200);
            entity.Property(questionnaire => questionnaire.Tags).HasMaxLength(500);
            entity.HasMany(questionnaire => questionnaire.Questions)
                .WithOne(question => question.Questionnaire)
                .HasForeignKey(question => question.QuestionnaireId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(questionnaire => questionnaire.Submissions)
                .WithOne(submission => submission.Questionnaire)
                .HasForeignKey(submission => submission.QuestionnaireId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Text).HasMaxLength(500);
            entity.Property(question => question.AnswerType)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.HasIndex(question => new { question.QuestionnaireId, question.SortOrder });
        });

        modelBuilder.Entity<QuestionnaireSubmission>(entity =>
        {
            entity.HasKey(submission => submission.Id);
            entity.HasMany(submission => submission.Answers)
                .WithOne(answer => answer.QuestionnaireSubmission)
                .HasForeignKey(answer => answer.QuestionnaireSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionAnswer>(entity =>
        {
            entity.HasKey(answer => answer.Id);
            entity.Property(answer => answer.TextValue).HasMaxLength(4000);
            entity.HasOne(answer => answer.Question)
                .WithMany(question => question.Answers)
                .HasForeignKey(answer => answer.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
