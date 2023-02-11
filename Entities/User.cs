using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

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


    }
}

