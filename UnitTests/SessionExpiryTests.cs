using System.Net;
using System.Security.Claims;
using Druware.Server;
using Druware.Server.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RESTfulFoundation.Server;

namespace UnitTests;

/// <summary>
/// Exercises CustomController.UpdateUserAccess against a real Identity stack
/// backed by an in-memory Sqlite ServerContext.
/// </summary>
[TestFixture]
public class SessionExpiryTests
{
    private const string UserName = "session.user@example.com";

    private SqliteConnection _connection = null!;
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IConfiguration _configuration = null!;

    private UserManager<User> UserManager =>
        _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // ServerContext.OnModelCreating reads the provider from AppSettings.
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API:Database:ConnectionType"] = "SQLITE",
                ["API:Database:ConnectionString"] = "DataSource=:memory:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_configuration);
        services.AddDbContext<ServerContext>(o => o.UseSqlite(_connection));
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        services.AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ServerContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _scope.ServiceProvider.GetRequiredService<ServerContext>()
            .Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    private async Task<User> CreateUser(DateTime? sessionExpires)
    {
        var user = new User
        {
            UserName = UserName,
            Email = UserName,
            SessionExpires = sessionExpires
        };
        var result = await UserManager.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.That(result.Succeeded, Is.True,
            string.Join("; ", result.Errors.Select(e => e.Description)));
        return user;
    }

    private TestController NewController()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, UserName)],
                IdentityConstants.ApplicationScheme))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        httpContext.Features.Set<IRoutingFeature>(
            new RoutingFeature { RouteData = new RouteData() });

        var signInManager =
            _scope.ServiceProvider.GetRequiredService<SignInManager<User>>();
        signInManager.Context = httpContext;

        return new TestController(_configuration, UserManager, signInManager,
            _scope.ServiceProvider.GetRequiredService<ServerContext>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Test]
    public async Task UpdateUserAccess_ExpiredSession_SignsOutAndReturns401()
    {
        await CreateUser(DateTime.UtcNow.AddMinutes(-1));
        var controller = NewController();

        var response = await controller.CallUpdateUserAccess();

        Assert.That(response, Is.TypeOf<ObjectResult>());
        var unauthorized = (ObjectResult)response!;
        Assert.That(unauthorized.Value, Is.TypeOf<Result>());
        var result = (Result)unauthorized.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(unauthorized.StatusCode,
                Is.EqualTo(StatusCodes.Status401Unauthorized));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Info,
                Does.Contain("Session has expired, user must log in."));
            // SignOutAsync clears the Identity application cookie.
            Assert.That(
                controller.HttpContext.Response.Headers["Set-Cookie"].ToString(),
                Does.Contain(".AspNetCore.Identity.Application="));
        });
    }

    [Test]
    public async Task UpdateUserAccess_ActiveSession_ReturnsNullAndExtendsSession()
    {
        var expires = DateTime.UtcNow.AddMinutes(5);
        await CreateUser(expires);
        var controller = NewController();

        var response = await controller.CallUpdateUserAccess();

        var user = await UserManager.FindByNameAsync(UserName);
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Null);
            Assert.That(user!.SessionExpires, Is.GreaterThan(expires));
            Assert.That(
                controller.HttpContext.Response.Headers["Set-Cookie"].ToString(),
                Is.Empty);
        });
    }

    [Test]
    public async Task UpdateUserAccess_UnknownUser_StillReturns200Error()
    {
        var response = await NewController().CallUpdateUserAccess();

        Assert.That(response, Is.TypeOf<OkObjectResult>());
        var result = (Result)((OkObjectResult)response!).Value!;
        Assert.That(result.Info, Does.Contain("Unable to find User"));
    }

    private sealed class TestController(
        IConfiguration configuration,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ServerContext context)
        : CustomController(configuration, userManager, signInManager, context)
    {
        public Task<ActionResult?> CallUpdateUserAccess() => UpdateUserAccess();
    }
}
