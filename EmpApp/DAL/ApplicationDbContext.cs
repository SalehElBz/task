using EmpApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmpApp.DAL;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<JobPosition> JobPositions { get; set; } = null!;
    public DbSet<UploadedFile> uploadedFiles { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // // builder.Entity<IdentityUserLogin<string>>().HasNoKey();
        // // builder.Entity<IdentityUserRole<string>>().HasNoKey();
        // // builder.Entity<IdentityUserToken<string>>().HasNoKey();
        // builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}