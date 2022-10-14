using System;
using System.Collections;
using System.Text.Json.Serialization;

namespace Druware.Server.Results
{
    public class ListResult : Result
    {
        public static new ListResult Error(string? message = null)
        {
            ListResult result = new()
            {
                Succeeded = false,
                Message = message,
                Errors = new List<string>()
            };
            return result;
        }

        public static ListResult Ok(
            long total, int page, int perPage, IList list,
            string? message = null)
        {
            ListResult result = new()
            {
                Succeeded = true,
                Message = message,
                Page = page,
                TotalRecords = total,
                PerPage = perPage,
                List = list
            };
            return result;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? TotalRecords { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Page { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PerPage { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList? List { get; set; }


    }
}

