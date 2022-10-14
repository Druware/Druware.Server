using System;
using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;
using Druware.Server.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Web;

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        private readonly ApplicationSettings _settings;

        public UserController(
            IConfiguration configuration,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _configuration = configuration;
            _settings = new ApplicationSettings(_configuration);
            _mapper = mapper;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return Ok(new Models.UserRegistrationModel());
        }

        [HttpGet("Valid")]
        public IActionResult Validate()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            return (_signInManager.IsSignedIn(currentUser)) ?
                Ok(Result.Ok("")) : Ok(Result.Error(""));
        }

        [HttpPost("Login")]
        public async Task<ActionResult<Result>> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null) return Ok(Result.Error("Acocunt Not Found"));

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (!result.Succeeded) return Ok(Result.Error("Account Not Found"));

            // If this is the first and only account in the system, promote it
            // to administration
            if (_userManager.Users.Count() == 1)
            {
                await _userManager.RemoveFromRoleAsync(user, "Visitor");
                await _userManager.AddToRoleAsync(user, "Administrator");
            }

            // All done
            return Ok(Result.Ok("Logged In"));
        }

        [HttpDelete("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(Result.Ok("Logged Out"));
        }

        [HttpGet("Register")]
        public async Task<ActionResult<Results.RegistrationResult>> ConfirmEmail([FromQuery] string token, [FromQuery] string email)
        {
            // Confirm the Email
            var user = await _userManager.FindByEmailAsync(email);
            //var decoded = HttpUtility.UrlDecode(token);
            if (user == null)
                return Ok(Result.Error("Cannot Confirm This Account"));
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded) return Ok(Result.Ok("Account Confirmed"));

            var errorResult = Result.Error("Cannot Confirm This Account");
            foreach (var error in result.Errors)
                errorResult.Errors?.Add(error.Description);
            return Ok(errorResult);
        }

        [HttpPost("Register")]
        public async Task<ActionResult<RegistrationResult>> Register(
            [FromBody] UserRegistrationModel model)
        {
            if (!ModelState.IsValid)
                return Ok(Results.RegistrationResult.Error("Invalid Model Recieved"));

            var user = _mapper.Map<User>(model);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                Results.RegistrationResult errorResult = Results.RegistrationResult.Error("Unable to create user.");
                foreach (var error in result.Errors)
                    errorResult.Errors?.Add(error.Description);
                return Ok(errorResult);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // build out the email message to the registered email with the
            // confirmation link that provides the path to confirm the email.

            if (_settings.Smtp == null) throw new Exception("Mail Services Not  Configured");

            var link = string.Format("{0}?email={2}&token={1}",
                _settings.ConfirmationUrl,
                HttpUtility.UrlEncode(token),
                user.Email);

            // TODO: Flesh this out to provide a nice email for confirmation,
            //       preferably with multiple output formats ( templaste loaded from resources perhaps )
            if (Assembly.GetEntryAssembly()?.GetName()?.Name == null)
                return Ok(RegistrationResult.Error("Assembly Not Found"));
            if (_settings.Notification == null)
                return Ok(RegistrationResult.Error("Settings Not Found"));

            MailHelper helper = new MailHelper(_settings.Smtp, Assembly.GetEntryAssembly()!.GetName()!.Name!);
            helper.Send(
                user.Email,
                _settings.Notification.From,
                _settings.Notification.From,
                "Confirmation email link",
                link!
            );

            await _userManager.AddToRoleAsync(user, "Visitor");

            return Ok(Results.RegistrationResult.Ok("User Created, limited to Visitor Status Until Confirmed."));
        }
    }
}

