using Microsoft.EntityFrameworkCore;
using ConwaysGameOfLifeApi.Models.Domain;

namespace ConwaysGameOfLifeApi.Data;

public class ApplicationDbContext : DbContext
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
  {
  }

  public DbSet<Board> Boards { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Board>(entity =>
    {
      entity.HasKey(e => e.Id);

      entity.Property(e => e.Id)
              .ValueGeneratedOnAdd();

      entity.Property(e => e.Width)
              .IsRequired();

      entity.Property(e => e.Height)
              .IsRequired();

      entity.Property(e => e.BoardData)
              .IsRequired()
              .HasColumnType("jsonb"); // Use JSONB for efficient JSON storage in PostgreSQL

      entity.Property(e => e.CreatedAt)
              .IsRequired()
              .HasDefaultValueSql("NOW()");

      entity.HasIndex(e => e.CreatedAt);
    });
  }
}
