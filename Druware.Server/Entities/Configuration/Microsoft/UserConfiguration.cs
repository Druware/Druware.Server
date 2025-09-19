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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Druware.Server.Entities.Configuration.Microsoft
{
    /// <summary>
    /// Configure the User object as the foundation of all authentication
    /// activities. The User object extends the Identity Framework AspNetUser/
    /// IdentityUser object.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        /// <summary>
        /// Called from the Context's OnModelCreateing() method, which in turn
        /// calls the ModelBuilder.ApplyConfiguration(new UserConfiguration());
        ///
        /// This configuration creates database backed storage of the entity
        /// </summary>
        /// <param name="entity"></param>
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.ToTable("user", "auth");
            
            entity.Property(e => e.Registered)
                .HasColumnName("Registered")
                .HasDefaultValueSql("getDate()"); // Microsoft version

            entity.Property(e => e.SessionExpires)
                .HasColumnName("SessionExpires");

            entity.HasKey(e => e.Id);
        }
    }
}

