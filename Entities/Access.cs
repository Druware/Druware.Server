﻿/* This file is part of the Druware.Server API Library
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

namespace Druware.Server.Entities
{
    /// <summary>
    /// The Access entity represents a single line item in the Access Log that
    /// shows who accessed what resource, when for auditing and analysis
    /// purposes
    /// </summary>
    public class Access
    {
        public long Id { get; set; }
        public string? Who { get; set; }
        public string? What { get; set; }
        public string? Where { get; set; }
        public DateTime When { get; set; } = DateTime.UtcNow;
        public string? How { get; set; }
    }
}

