using System;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Druware.Server
{
    public static class DbContextExtensions
    {
        public static IQueryable<T> TagWithSource<T>(this IQueryable<T> queryable,
            string tag = "",
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int line = 0)
        {
            return queryable.TagWith(string.IsNullOrEmpty(tag)
                ? $"{methodName} - {sourceFilePath}:{line}"
                : $"{tag}{Environment.NewLine}{methodName}  - {sourceFilePath}:{line}");
        }
    }
}

