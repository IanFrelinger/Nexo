using Microsoft.EntityFrameworkCore;
using FeatureFactoryDemo.Models;

namespace FeatureFactoryDemo.Data
{
    /// <summary>
    /// Database context for Feature Factory demo data
    /// </summary>
    public class FeatureFactoryDbContext : DbContext
    {
        public FeatureFactoryDbContext(DbContextOptions<FeatureFactoryDbContext> options) : base(options)
        {
        }
        
        public DbSet<CommandHistory> CommandHistories { get; set; }
        public DbSet<CodebaseContext> CodebaseContexts { get; set; }
        public DbSet<CodePattern> CodePatterns { get; set; }
        public DbSet<E2ETestHistory> E2ETestHistories { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure CommandHistory
            modelBuilder.Entity<CommandHistory>(entity =>
            {
                entity.HasIndex(e => e.ExecutedAt);
                entity.HasIndex(e => e.Platform);
                entity.HasIndex(e => e.FinalQualityScore);
                entity.Property(e => e.GeneratedCode).HasColumnType("TEXT");
                entity.Property(e => e.Context).HasColumnType("TEXT");
            });
            
            // Configure CodebaseContext
            modelBuilder.Entity<CodebaseContext>(entity =>
            {
                entity.HasIndex(e => e.FilePath).IsUnique();
                entity.HasIndex(e => e.FileType);
                entity.HasIndex(e => e.LastAnalyzed);
                entity.Property(e => e.Content).HasColumnType("TEXT");
                entity.Property(e => e.Violations).HasColumnType("TEXT");
            });
            
            // Configure CodePattern
            modelBuilder.Entity<CodePattern>(entity =>
            {
                entity.HasIndex(e => e.PatternType);
                entity.HasIndex(e => e.UsageCount);
                entity.HasIndex(e => e.SuccessRate);
                entity.Property(e => e.Template).HasColumnType("TEXT");
            });
            
            // Configure E2ETestHistory
            modelBuilder.Entity<E2ETestHistory>(entity =>
            {
                entity.HasIndex(e => e.Platform);
                entity.HasIndex(e => e.GeneratedAt);
                entity.HasIndex(e => e.ExecutedAt);
                entity.HasIndex(e => e.IsSuccessful);
                entity.Property(e => e.GeneratedCode).HasColumnType("TEXT");
                entity.Property(e => e.TestSuite).HasColumnType("TEXT");
                entity.Property(e => e.TestResult).HasColumnType("TEXT");
            });
        }
    }
}
