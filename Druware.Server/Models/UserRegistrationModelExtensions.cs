using System;
using Druware.Server.Entities;

namespace Druware.Server.Models
{
    public static class UserRegistrationModelExtensions
    {
        /// <summary>
        /// Projects the registration model onto a new User entity. The Email
        /// doubles as the UserName, as the server does not carry a separate
        /// login name.
        /// </summary>
        public static User ToUser(this UserRegistrationModel model) =>
            model.ApplyTo(new User());

        /// <summary>
        /// Applies the registration model to an existing User entity, leaving
        /// any property the model does not carry untouched.
        /// </summary>
        public static User ApplyTo(this UserRegistrationModel model, User user)
        {
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            return user;
        }
    }
}