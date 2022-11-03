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
using Microsoft.AspNetCore.Authorization;
using System.Data;

// TODO: Split this into the reperate controllers

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

        // this should handle all the profile information as well as password
        // resets.

        // get - return the current user profile, if the user is the current user
        [Authorize]
        [HttpGet("")]
        public IActionResult GetProfile()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            return (_signInManager.IsSignedIn(currentUser)) ?
                Ok(Result.Ok("")) : Ok(Result.Error(""));
        }

        // post - trigger a password reset
        // get / password - handle a password reset request
        // put - update the user profile

    }
}

