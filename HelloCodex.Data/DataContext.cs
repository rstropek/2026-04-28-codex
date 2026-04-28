using Microsoft.EntityFrameworkCore;

namespace HelloCodex.Data;

public sealed class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Questionnaire> Questionnaires { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Questionnaire>(entity =>
        {
            entity.HasKey(questionnaire => questionnaire.Id);
            entity.Property(questionnaire => questionnaire.Code).HasMaxLength(50);
            entity.Property(questionnaire => questionnaire.Description).HasMaxLength(200);
        });
    }
}
