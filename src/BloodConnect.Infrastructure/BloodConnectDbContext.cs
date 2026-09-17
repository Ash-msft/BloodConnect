using BloodConnect.Domain;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Infrastructure;

public class BloodConnectDbContext : DbContext
{
    public BloodConnectDbContext(DbContextOptions<BloodConnectDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<DonorProfile> DonorProfiles => Set<DonorProfile>();

    public DbSet<DonationHistoryEntry> DonationHistoryEntries => Set<DonationHistoryEntry>();

    public DbSet<BloodRequest> BloodRequests => Set<BloodRequest>();

    public DbSet<DonorResponse> DonorResponses => Set<DonorResponse>();

    public DbSet<NotificationOutboxItem> NotificationOutboxItems => Set<NotificationOutboxItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.ExternalId).IsUnique();
            entity.Property(u => u.ExternalId).IsRequired().HasMaxLength(200);
            entity.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
        });

        modelBuilder.Entity<DonorProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.Property(p => p.City).IsRequired().HasMaxLength(120);
            entity.Property(p => p.Pincode).HasMaxLength(10);
            entity.HasOne(p => p.User)
                .WithOne(u => u.DonorProfile)
                .HasForeignKey<DonorProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.DonationHistory)
                .WithOne(h => h.DonorProfile)
                .HasForeignKey(h => h.DonorProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DonationHistoryEntry>(entity =>
        {
            entity.Property(h => h.Notes).HasMaxLength(1000);
        });

        modelBuilder.Entity<BloodRequest>(entity =>
        {
            entity.Property(r => r.HospitalName).IsRequired().HasMaxLength(200);
            entity.Property(r => r.City).IsRequired().HasMaxLength(120);
            entity.Property(r => r.Pincode).HasMaxLength(10);
            entity.Property(r => r.Notes).HasMaxLength(1000);
            entity.HasOne(r => r.Requester)
                .WithMany()
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(r => r.Responses)
                .WithOne(resp => resp.BloodRequest)
                .HasForeignKey(resp => resp.BloodRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DonorResponse>(entity =>
        {
            entity.HasIndex(r => new { r.BloodRequestId, r.DonorUserId }).IsUnique();
            entity.HasOne(r => r.DonorUser)
                .WithMany()
                .HasForeignKey(r => r.DonorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationOutboxItem>(entity =>
        {
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Body).IsRequired().HasMaxLength(2000);
            entity.Property(n => n.Channel).IsRequired().HasMaxLength(50);
            entity.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
