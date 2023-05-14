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
        .HasForeignKey(fk => fk.JobPositionID);
        builder.HasOne<UploadedFile>()
        .WithOne()
        .HasForeignKey<UploadedFile>(e => e.Id);
    }
}