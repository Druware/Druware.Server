using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Druware.Server.Entities.Configuration
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasData(
                new Role
                {
                    Description = "Unconfirmed User",
                    Name = UserSecurityRole.Unconfirmed,
                    NormalizedName = UserSecurityRole.Unconfirmed.ToUpper()
                },
                new Role
                {
                    Description = "Confirmed User ",
                    Name = UserSecurityRole.Confirmed,
                    NormalizedName = UserSecurityRole.Confirmed.ToUpper()
                },
                new Role
                {
                    Description = "User Manager",
                    Name = UserSecurityRole.Manager,
                    NormalizedName = UserSecurityRole.Manager.ToUpper()
                },
                new Role
                {
                    Description = "System Administrator",
                    Name = UserSecurityRole.SystemAdministrator,
                    NormalizedName = UserSecurityRole.SystemAdministrator.ToUpper()
                }
            );
        }
    }
}

