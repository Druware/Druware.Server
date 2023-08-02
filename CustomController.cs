using System;
using AutoMapper;
using Druware.Server.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using RESTfulFoundation.Server;

namespace Druware.Server
{
	public class CustomController : ControllerBase
    {
        protected readonly IConfiguration Configuration;
        protected readonly UserManager<User> UserManager;
        protected readonly SignInManager<User> SignInManager;
        protected readonly ServerContext ServerContext;

        public CustomController(
            IConfiguration configuration,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ServerContext context)
        {
            Configuration = configuration;
            SignInManager = signInManager;
            UserManager = userManager;
            ServerContext = context;
        }

        protected async Task LogRequest()
        {
            User? user = await Entities.User.ByName(this.User.Identity?.Name, UserManager);

            RouteData data = HttpContext.GetRouteData();
            string? what = string.Format("{0}.{1}",
                data.Values["controller"]?.ToString() ?? "",
                data.Values["action"]?.ToString() ?? "");

            string? where = HttpContext.Connection.RemoteIpAddress.ToString();
            Access access = new();
            access.Who = user?.UserName ?? "anonymous";
            access.When = DateTime.UtcNow;
            access.What = what;
            access.How = HttpContext.Request.Method ?? "";
            access.Where = where;

            ServerContext.AccessLog.Add(access);
            await ServerContext.SaveChangesAsync();
        }

        protected async Task<ActionResult?> UpdateUserAccess()
        {
            User? user = await Entities.User.ByName(this.User.Identity?.Name, UserManager);
            // theoretically, this should never get here, but we perform the
            // check just to be safe.
            if (user == null) return Ok(Result.Error("Unable to find User"));

            if (user.IsSessionExpired())
            {
                // log the user out, and move on
                await SignInManager.SignOutAsync();
                return Ok(Result.Error("Session has expired, user must log in."));
            }

            // get the route for the 'Where', and the method for the 'How'
            RouteData data = HttpContext.GetRouteData();
            string? what = string.Format("{0}.{1}",
                data.Values["controller"]?.ToString() ?? "",
                data.Values["action"]?.ToString() ?? "");

            string? where = HttpContext.Connection.RemoteIpAddress.ToString();

            await user!.UpdateAccessed(ServerContext,
                what,
                HttpContext.Request.Method ?? "", // how
                where //  where
            );
            return null;
        }


    }
}

