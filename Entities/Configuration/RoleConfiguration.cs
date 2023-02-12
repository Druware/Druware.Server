/* This file is part of the Druware.Server API Library
 * 
 * Foobar is free software: you can redistribute it and/or modify it under the 
 * terms of the GNU General Public License as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later 
 * version.
 * 
 * The Druware.Server API Library is distributed in the hope that it will be 
 * useful, but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General 
 * Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along with 
 * the Druware.Server API Library. If not, see <https://www.gnu.org/licenses/>.
 * 
 * Copyright 2019-2023 by:
 *    Andy 'Dru' Satori @ Satori & Associates, Inc.
 *    All Rights Reserved
 */

using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Druware.Server.Entities.Configuration
{
    /// <summary>
    /// Configure the User object as the foundation of all authentication
    /// activities. The Role object extends the Identity Framework AspNetRole/
    /// IdentityRole object.
    /// </summary>
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        /// <summary>
        /// Called from the Context's OnModelCreateing() method, which in turn
        /// calls the ModelBuilder.ApplyConfiguration(new UserConfiguration());
        ///
        /// This configuration creates database backed storage of the entity
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role", "auth");

            // This Unique Index speeds lookup, but also prevents duplicates
            builder.HasIndex(e => e.Description)
                .IsUnique();

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

