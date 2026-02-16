// DSC TLink - a communications library for DSC Powerseries NEO alarm panels
// Copyright (C) 2024 Brian Humlicek
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace DSC.TLink.Serialization
{
    /// <summary>
    /// Marks a string property as BCD (Binary Coded Decimal) encoded.
    /// Each byte stores two decimal digits (one per nibble).
    /// Unused trailing nibbles are padded with zeros on write.
    /// Trailing zero digits are trimmed on read.
    /// </summary>
    /// <remarks>
    /// Two modes are supported:
    /// <list type="bullet">
    /// <item><b>Fixed length:</b> <c>[BCDString(2)]</c> — always serializes to exactly N bytes.</item>
    /// <item><b>Unbounded:</b> <c>[BCDString]</c> — consumes all remaining bytes on read. Must be the last property.</item>
    /// </list>
    /// For length-prefixed BCD strings, use <see cref="LeadingLengthBCDStringAttribute"/> instead.
    /// </remarks>
    /// <example>
    /// // Fixed: 4-digit access code stored in 2 BCD bytes
    /// [BCDString(2)]
    /// public string AccessCode { get; init; } = "";
    /// 
    /// // Unbounded: consumes remaining bytes (must be last property)
    /// [BCDString]
    /// public string TrailingDigits { get; init; } = "";
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BCDStringAttribute : Attribute
    {
        /// <summary>
        /// The fixed number of BCD bytes in the serialized format, or null for unbounded mode.
        /// Each byte holds two decimal digits.
        /// </summary>
        public int? FixedLength { get; }

        /// <summary>
        /// Create an unbounded BCD string attribute that consumes all remaining bytes.
        /// Must be the last serialized property on the message.
        /// </summary>
        public BCDStringAttribute()
        {
            FixedLength = null;
        }

        /// <summary>
        /// Create a fixed-length BCD string attribute.
        /// </summary>
        /// <param name="length">Number of BCD bytes (each stores 2 digits)</param>
        public BCDStringAttribute(int length)
        {
            if (length <= 0)
                throw new ArgumentException("BCD array length must be greater than zero", nameof(length));

            FixedLength = length;
        }
    }
}
