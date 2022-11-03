using System;
using System.Text.Json.Serialization;

namespace Druware.Server.Results
{
    public class Result
    {
        public static Result Error(List<string>? info = null)
        {
            Result result = new()
            {
                Succeeded = false,
                Info = info == null ? new() : info
            };
            return result;
        }

        public static Result Error(string? info = null)
        {
            Result result = new()
            {
                Succeeded = false,
                Info = new()
            };
            if (info != null)
                result.Info!.Add(info);
            return result;
        }

        public static Result Ok(
            List<string>? info = null)
        {
            Result result = new()
            {
                Succeeded = true,
                Info = info == null ? new() : info
            };
            return result;
        }

        public static Result Ok(string? info = null)
        {
            Result result = new()
            {
                Succeeded = true,
                Info = new()
            };
            if (info != null)
                result.Info!.Add(info);
            return result;
        }

        public bool Succeeded { get; set; } = false;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Info { get; set; } = null;
    }
}

