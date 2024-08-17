using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieMunch.Context.Models;

namespace MovieMunch.EntityConfigurations
{
    public class TblusermasterConfiguration : IEntityTypeConfiguration<Tblusermaster>
    {
        public void Configure(EntityTypeBuilder<Tblusermaster> builder)
        {
            builder.ToTable("Tblusermaster");

            builder.HasKey(t => t.UserID);

            builder.Property(t => t.UserName)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(t => t.Password)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(t => t.Email)
                   .HasMaxLength(100);

            builder.Property(t => t.PhoneNumber)
                   .HasMaxLength(15);

            builder.Property(t => t.IsActive)
                   .IsRequired();
        }
    }
}
