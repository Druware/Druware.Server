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

// After an enormous amount of thought, this needs to be reworked to be smart 
// to support both DbTypes internally, and the platform specific classes 
// exist only for migrations.

using Druware.Server.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Druware.Server.Entities.Configuration;

namespace Druware.Server;

// TODO: Add an Access Log for user auditing purposes

public interface IServerContext
{
    public abstract DbSet<Access> AccessLog { get; set; }
    public abstract DbSet<Tag> Tags { get; set; }
}

public class ServerContext : IdentityDbContext<User, IdentityRole, string>, IServerContext
{
    private readonly IConfiguration? _configuration;
    
    public ServerContext() { }

    public ServerContext(IConfiguration? configuration) : base()
    {
        _configuration = configuration;
    }

    public ServerContext(DbContextOptions<ServerContext> options, IConfiguration? configuration)
        : base(options)
    {
        _configuration = configuration;
    }

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
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
        if (_configuration != null)
        {
            var settings = new AppSettings(_configuration!);
            switch (settings.DbType)
            {
                case DbContextType.Microsoft:
                    if (settings.ConnectionString != null)
                        optionsBuilder.UseSqlServer(settings.ConnectionString);
                    break;
                case DbContextType.PostgreSql:
                    if (settings.ConnectionString != null)
                        optionsBuilder.UseNpgsql(settings.ConnectionString);
                    break;
                    default:
                    throw new Exception(
                        "There is no configuration for this DbType");
            }
            if (settings.ConnectionString != null)
                optionsBuilder.UseSqlServer(settings.ConnectionString);
            return;
        }
        
    }

    /// <summary>
    /// Used for configuring the model and database contexts for use in the
    /// application. 
    /// </summary>
    /// <param name="builder"></param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        Console.WriteLine("Initializing ServerContext");
        base.OnModelCreating(builder);

        var settings = new AppSettings(_configuration!);

        switch (settings.DbType)
        {
            case DbContextType.Microsoft:
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Microsoft.
                        UserConfiguration());
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Microsoft.
                        RoleConfiguration());

                // Druware.Server Entities
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Microsoft.
                        AccessConfiguration());
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Microsoft.
                        TagConfiguration());
                break;
            case DbContextType.PostgreSql:
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.PostgreSql.
                        UserConfiguration());
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.PostgreSql.
                        RoleConfiguration());

                // Druware.Server Entities
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.PostgreSql.
                        AccessConfiguration());
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.PostgreSql.
                        TagConfiguration());
                break;
            
            case DbContextType.Sqlite:
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Sqlite.
                        UserConfiguration());
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Sqlite.
                        RoleConfiguration());

                // Druware.Server Entities
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Sqlite.
                        AccessConfiguration());
                builder.ApplyConfiguration(
                    new Druware.Server.Entities.Configuration.Sqlite.
                        TagConfiguration());
                break;
            default:
                throw new Exception(
                    "There is no configuration for this DbType");
        }
    }

    #endregion
}
