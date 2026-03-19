using CLM.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CLM.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role
        modelBuilder.Entity<Role>(e => {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Name).IsUnique();
        });

        // User
        modelBuilder.Entity<User>(e => {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
        });

        // Contract
        modelBuilder.Entity<Contract>(e => {
            e.HasKey(c => c.Id);
            e.Property(c => c.Title).IsRequired().HasMaxLength(300);
            e.Property(c => c.Value).HasPrecision(18, 2);
            e.Property(c => c.Status).HasConversion<string>();
            e.HasOne(c => c.CreatedBy).WithMany(u => u.CreatedContracts).HasForeignKey(c => c.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ContractVersion
        modelBuilder.Entity<ContractVersion>(e => {
            e.HasKey(cv => cv.Id);
            e.HasOne(cv => cv.Contract).WithMany(c => c.Versions).HasForeignKey(cv => cv.ContractId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cv => cv.ChangedBy).WithMany().HasForeignKey(cv => cv.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Approval
        modelBuilder.Entity<Approval>(e => {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasConversion<string>();
            e.HasOne(a => a.Contract).WithMany(c => c.Approvals).HasForeignKey(a => a.ContractId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Approver).WithMany(u => u.Approvals).HasForeignKey(a => a.ApproverId).OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e => {
            e.HasKey(al => al.Id);
            e.HasOne(al => al.User).WithMany(u => u.AuditLogs).HasForeignKey(al => al.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(al => al.Contract).WithMany(c => c.AuditLogs).HasForeignKey(al => al.EntityId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Manager" },
            new Role { Id = 3, Name = "User" }
        );
    }
}
