using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Druware.Server.Entities.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User", "Customer");
            //builder.HasMany(e => e.Organizations)
            //    .WithOne(o => o.Owner)
            //    .HasForeignKey(s => s.OwnerId);
        }
    }
}

