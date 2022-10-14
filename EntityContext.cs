using System;
using Druware.Server.Entities;
using Druware.Server.Entities.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Druware.Server
{
    public class EntityContext : IdentityDbContext<User, IdentityRole, string>
    {
        // a generic default, needs an ifdef option for switching contexts
        private const string cs = "Host=localhost;Database=druware;Username=postgres;Password=notforproduction";

        public EntityContext() : base() { }

        public EntityContext(DbContextOptions<EntityContext> options)
            : base(options)
        {

        }

        /// <summary>
        /// Configure the User Context to use the database as defined by the
        /// ApplicationSettings
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(cs); // this is the default
                // optionsBuilder.UseSqlServer(cs);
            }
        }

        /// <summary>
        /// Used for configuring the model and database contexts for use in the
        /// application. 
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new RoleConfiguration());

            // Ignored Classes
            // builder.Ignore<NotMappedClass>();
        }

        // Add my DB Sets

        // public DbSet<Foo>? Bar { get; set; }

    }
}
