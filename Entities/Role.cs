using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace Druware.Server.Entities
{
	public class Role : IdentityRole
	{
		public Role()
		{
		}

		public string? Description { get; set; }
        [JsonIgnore]
        public override string? ConcurrencyStamp { get; set; }
    }
}

