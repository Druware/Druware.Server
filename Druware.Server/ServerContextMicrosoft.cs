using Druware.Server.Entities;
using Druware.Server.Entities.Configuration.Microsoft;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Druware.Server;

public class ServerContextMicrosoft : IdentityDbContext<User, IdentityRole, string>, IServerContext
{
    private readonly IConfiguration? _configuration;
    
    public ServerContextMicrosoft() : base()
    {
        
    }
    
    public ServerContextMicrosoft(IConfiguration? configuration) : base()
    {
        _configuration = configuration;
    }

    public ServerContextMicrosoft(DbContextOptions<ServerContextMicrosoft> options, IConfiguration? configuration)
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
            if (settings.ConnectionString != null)
                optionsBuilder.UseSqlServer(settings.ConnectionString);
            return;
        }

#if DEBUG
        // this is required to run any migration generation.  By default, we
        // leave it empty, and only populate it for generating migrations.
        const string cs = "";
        optionsBuilder.UseSqlServer(cs);
#endif

    }

    #endregion

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
}