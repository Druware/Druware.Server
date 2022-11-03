using System;
using System.Runtime.Serialization;
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
    }
}

