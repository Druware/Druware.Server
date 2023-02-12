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
using Druware.Server.Entities.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Druware.Server
{
    // TODO: Add an Access Log for user auditing purposes

    public class ServerContext : IdentityDbContext<User, IdentityRole, string>
    {
#if DEBUG
        private const string cs = "Host=localhost;Database=druware;Username=postgres;Password=notforproduction";
#endif

        public ServerContext() : base() { }

        public ServerContext(DbContextOptions<ServerContext> options)
            : base(options) { }

        #region Properties

        public virtual DbSet<Access> AccessLog { get; set; } = null!;
        public virtual DbSet<Tag> Tags { get; set; } = null!;

        #endregion

        #region Configuration

        /// <summary>
        /// Configure the User Context to use the database as defined by the
        /// ApplicationSettings
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            // We *should* never get here, and only in DEBUG mode
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql(cs); // this is the default
#else
            throw new Exception("No Connection String Provided");
#endif
        }

        /// <summary>
        /// Used for configuring the model and database contexts for use in the
        /// application. 
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Identity Framework Customizations
            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new RoleConfiguration());

            // Druware.Server Entities
            builder.ApplyConfiguration(new AccessConfiguration());
            builder.ApplyConfiguration(new TagConfiguration());


            // Ignored Classes
            // builder.Ignore<NotMappedClass>();
        }

#endregion
    }
}
