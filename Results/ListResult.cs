using System;
using System.Collections;
using System.Text.Json.Serialization;

namespace Druware.Server.Results
{
    public class ListResult : Result
    {
        public static ListResult Ok(
            long total, int page, int perPage, IList list,
            string? info = null)
        {
            ListResult result = new()
            {
                Succeeded = true,
                Info = null,
                Page = page,
                TotalRecords = total,
                PerPage = perPage,
                List = list
            };
            if (info != null)
            {
                result.Info = new();
                result.Info!.Add(info);
            }
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

