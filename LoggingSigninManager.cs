using System;
using Druware.Server.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Druware.Server
{
    
    public class LoggingSignInManager<TUser> : SignInManager<User> where TUser : class
	{
        private readonly UserManager<User> _userManager;
        private readonly ServerContext _db;
        private readonly IHttpContextAccessor _contextAccessor;

        public LoggingSignInManager(
            UserManager<User> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<User> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<User>> logger,
            ServerContext dbContext,
            IAuthenticationSchemeProvider schemeProvider
            )
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemeProvider)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public override Task<SignInResult> PasswordSignInAsync(string userName, string password, bool rememberMe, bool shouldLockout)
        {
            var user = UserManager.FindByEmailAsync(userName).Result;

            //if ((user.IsEnabled.HasValue && !user.IsEnabled.Value) || !user.IsEnabled.HasValue)
            //{
            //    return Task.FromResult(SignInResult.LockedOut);
            //}

            return base.PasswordSignInAsync(userName, password, rememberMe, shouldLockout);
        }
    }
    
}

