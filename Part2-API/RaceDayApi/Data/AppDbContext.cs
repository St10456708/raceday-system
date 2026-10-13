using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Users ----
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[role] IN ('Organiser','Participant')"));
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // ---- Events ----
        modelBuilder.Entity<RaceEvent>(entity =>
        {
            entity.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_Type", "[event_type] IN ('Run','Walk','Cycle')");
                t.HasCheckConstraint("CK_Events_Status", "[status] IN ('Upcoming','Completed','Cancelled')");
            });
            entity.Property(e => e.Status).HasDefaultValue("Upcoming");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // ---- Venues (1:1 with Events) ----
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasOne(v => v.Event)
                  .WithOne(e => e.Venue)
                  .HasForeignKey<Venue>(v => v.EventId);
            entity.HasIndex(v => v.EventId).IsUnique();
        });

        // ---- Categories ----
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Price).HasDefaultValue(0m);
            entity.Property(c => c.MaxParticipants).HasDefaultValue(100);
        });

        // ---- Enrolments ----
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[status] IN ('Confirmed','Cancelled')"));
            entity.Property(e => e.Status).HasDefaultValue("Confirmed");
            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique().HasDatabaseName("UQ_Enrolment_Once");
        });

        // ---- Results (1:1 with Enrolments) ----
        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[status] IN ('Finished','DNF','DNS')"));
            entity.HasOne(r => r.Enrolment)
                  .WithOne(e => e.Result)
                  .HasForeignKey<RaceResult>(r => r.EnrolmentId);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
            entity.Property(r => r.Status).HasDefaultValue("Finished");
            entity.Property(r => r.RecordedAt).HasDefaultValueSql("GETDATE()");
        });

        // The Part 1 SQL script uses plain FOREIGN KEY constraints (no ON DELETE CASCADE),
        // so make EF Core do the same.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}