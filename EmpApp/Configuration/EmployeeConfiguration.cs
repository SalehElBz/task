using EmpApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmpApp.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne<JobPosition>()
        .WithMany()
        .HasForeignKey(fk => fk.JobPositionID)
        .IsRequired();
        builder.HasOne<UploadedFile>()
        .WithOne()
        .HasForeignKey<ApplicationUser>(e => e.Id)
        .IsRequired();
    }
}