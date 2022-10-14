using System;
using System.Text.Json.Serialization;

namespace Druware.Server.Results
{
    public class Result
    {
        public static Result Error(string? message = null)
        {
            Result result = new()
            {
                Succeeded = false,
                Message = message,
                Errors = new List<string>()
            };
            return result;
        }

        public static Result Ok(
            string? message = null,
            string? token = null)
        {
            Result result = new()
            {
                Succeeded = true,
                Message = message
            };
            return result;
        }

        public bool Succeeded { get; set; } = false;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Errors { get; set; } = null;
    }
}

