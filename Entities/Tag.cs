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
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Druware.Server.Entities
{
    /// <summary>
    /// The Tag is a common way of tieing related items together with a loose
    /// relationship ( lazy, if you will ). Much like the #tag model found in
    /// social media, the tag pool is shared across multiple data elements so
    /// it now exists in the Server base, rather than being duplicated.
    /// </summary>
    public class Tag
    {
        public long? TagId { get; set; }
        public string? Name { get; set; } = null;

        public Tag(string name)
        {
            Name = name;
        }

        public static Tag ByNameOrId(ServerContext context, string value)
        {
            Int32 id;
            Tag? tag = null;

            if (Int32.TryParse(value, out id))
                tag = context.Tags?.SingleOrDefault(t => t.TagId == id);

            if (tag == null)
                tag = context.Tags?.SingleOrDefault(t => t.Name == value);

            if (tag == null)
            {
                tag = new(value);
                context.Tags!.Add(tag);
                context.SaveChanges();
            }

            return tag;
        }
    }
}

