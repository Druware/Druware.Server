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
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Druware.Server.Entities
{
    [DataContract]
    public class User : IdentityUser
    {
        public User()
        { }

        [DataMember]
        public string? FirstName { get; set; }
        [DataMember]
        public string? LastName { get; set; }

        // public virtual ICollection<Organization>? Organizations { get; set; } // Owned Organizations

        [JsonIgnore]
        public override string? PasswordHash { get; set; }
        [JsonIgnore]
        public override string? NormalizedUserName { get; set; }
        [JsonIgnore]
        public override string? NormalizedEmail { get; set; }
        [JsonIgnore]
        public override string? SecurityStamp { get; set; }
        [JsonIgnore]
        public override string? ConcurrencyStamp { get; set; }

        public DateTime? Registered { get; set; }
        public DateTime? SessionExpires { get; set; }

        public static async Task<User?> ByName(
            string? name,
            UserManager<User> userManager)
        {
            if (name == null) return null;
            return await userManager.FindByNameAsync(name);
        }
    }

    public static class UserExtensions
    {
        public static async Task UpdateAccessed(
            this User user,
            ServerContext context,
            string? what = null,
            string how = "GET",
            string? where = null)
        {
            if (what != null)
            {
                Access access = new();
                access.Who = user.UserName;
                access.When = DateTime.UtcNow;
                access.What = what;
                access.How = how;
                access.Where = where;

                context.AccessLog.Add(access);
            }

            user.SessionExpires = DateTime.UtcNow.AddMinutes(30);
            await context.SaveChangesAsync();
        }

        public static bool IsSessionExpired(this User user) =>
            user.SessionExpires < DateTime.UtcNow;
        
    }
}

