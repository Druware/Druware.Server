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
using Druware.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Druware.Server.Entities.Configuration.PostgreSql
{
    /// <summary>
    /// Configure the Tag object as a general use Tag pool throughout the system
    /// Tags may not have duplicate names, and WILL through an error if a
    /// duplicate as attemmpted to add. 
    /// </summary>
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        /// <summary>
        /// Called from the Context's OnModelCreateing() method, which in turn
        /// calls the ModelBuilder.ApplyConfiguration(new TagConfiguration());
        ///
        /// This configuration creates base tag pool that will be used thoughout
        /// </summary>
        /// <param name="entity"></param>
        public void Configure(EntityTypeBuilder<Tag> entity)
        {
            entity.ToTable("tag");

            entity.Property(e => e.TagId)
                .HasColumnName("tag_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(64);

            // This Unique Index speeds lookup, but also prevents duplicates
            entity.HasIndex(e => e.Name)
                .IsUnique();
        }
    }
}
