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

using System.Reflection;
using System.Text;

namespace DSC.TLink.Serialization
{
    /// <summary>
    /// Handles serialization of string properties with encoding-specific attributes:
    /// - [UnicodeString]: UTF-16LE encoded with a leading length prefix
    /// - [BCDString]: BCD encoded digit string, fixed length or unbounded
    /// - [LeadingLengthBCDString]: BCD encoded digit string with a 1-byte length prefix
    /// </summary>
    internal class StringSerializer : ITypeSerializer
    {
        public bool CanHandle(PropertyInfo property)
        {
            return property.PropertyType == typeof(string)
                && (property.IsDefined(typeof(UnicodeStringAttribute), false)
                 || property.IsDefined(typeof(BCDStringAttribute), false)
                 || property.IsDefined(typeof(LeadingLengthBCDStringAttribute), false));
        }

        public void Write(List<byte> bytes, PropertyInfo property, object? value)
        {
            var str = (string?)value ?? string.Empty;

            var unicodeAttr = property.GetCustomAttribute<UnicodeStringAttribute>();
            if (unicodeAttr != null)
            {
                WriteUnicodeString(bytes, property, str, unicodeAttr.LengthBytes);
                return;
            }

            var bcdAttr = property.GetCustomAttribute<BCDStringAttribute>();
            if (bcdAttr != null)
            {
                if (bcdAttr.FixedLength.HasValue)
                    WriteBCDStringFixed(bytes, str, bcdAttr.FixedLength.Value);
                else
                    WriteBCDStringUnbounded(bytes, str);
                return;
            }

            if (property.IsDefined(typeof(LeadingLengthBCDStringAttribute), false))
            {
                WriteBCDStringPrefixed(bytes, property, str);
                return;
            }
        }

        public object Read(ReadOnlySpan<byte> bytes, ref int offset, PropertyInfo property, int remainingBytes)
        {
            var unicodeAttr = property.GetCustomAttribute<UnicodeStringAttribute>();
            if (unicodeAttr != null)
            {
                return ReadUnicodeString(bytes, ref offset, property, unicodeAttr.LengthBytes);
            }

            var bcdAttr = property.GetCustomAttribute<BCDStringAttribute>();
            if (bcdAttr != null)
            {
                int bcdLength = bcdAttr.FixedLength ?? remainingBytes;
                return ReadBCDString(bytes, ref offset, property, bcdLength);
            }

            if (property.IsDefined(typeof(LeadingLengthBCDStringAttribute), false))
            {
                int bcdLength = ReadLengthPrefix1(bytes, ref offset, property);
                return ReadBCDString(bytes, ref offset, property, bcdLength);
            }

            throw new InvalidOperationException($"String property '{property.Name}' has no recognized encoding attribute.");
        }

        private static void WriteUnicodeString(List<byte> bytes, PropertyInfo property, string str, int lengthBytes)
        {
            var encoded = Encoding.Unicode.GetBytes(str);

            switch (lengthBytes)
            {
                case 1:
                    if (encoded.Length > 255)
                        throw new InvalidOperationException(
                            $"Property '{property.Name}' encoded string length {encoded.Length} exceeds 1-byte prefix max (255).");
                    bytes.Add((byte)encoded.Length);
                    break;

                case 2:
                    if (encoded.Length > 65535)
                        throw new InvalidOperationException(
                            $"Property '{property.Name}' encoded string length {encoded.Length} exceeds 2-byte prefix max (65535).");
                    bytes.Add((byte)(encoded.Length >> 8));
                    bytes.Add((byte)(encoded.Length & 0xFF));
                    break;

                default:
                    throw new InvalidOperationException($"Invalid length bytes {lengthBytes} for property '{property.Name}'");
            }

            bytes.AddRange(encoded);
        }

        private static string ReadUnicodeString(ReadOnlySpan<byte> bytes, ref int offset, PropertyInfo property, int lengthBytes)
        {
            int length = lengthBytes switch
            {
                1 => ReadLengthPrefix1(bytes, ref offset, property),
                2 => ReadLengthPrefix2(bytes, ref offset, property),
                _ => throw new InvalidOperationException($"Invalid length prefix size {lengthBytes} for property '{property.Name}'")
            };

            if (offset + length > bytes.Length)
                throw new InvalidOperationException(
                    $"Not enough bytes to read Unicode string '{property.Name}' (need {length}, have {bytes.Length - offset})");

            var str = Encoding.Unicode.GetString(bytes.Slice(offset, length));
            offset += length;
            return str;
        }

        private static void WriteBCDStringFixed(List<byte> bytes, string digits, int fixedLength)
        {
            // Pad with trailing zeros to fill the fixed byte length (2 digits per byte)
            var padded = digits.PadRight(fixedLength * 2, '0');

            for (int i = 0; i < fixedLength; i++)
            {
                byte highNibble = (byte)(padded[i * 2] - '0');
                byte lowNibble = (byte)(padded[i * 2 + 1] - '0');
                bytes.Add((byte)((highNibble << 4) | lowNibble));
            }
        }

        private static void WriteBCDStringUnbounded(List<byte> bytes, string digits)
        {
            // Pad to even number of digits
            if (digits.Length % 2 != 0)
                digits += '0';

            int bcdLength = digits.Length / 2;
            WriteBCDStringFixed(bytes, digits, bcdLength);
        }

        private static void WriteBCDStringPrefixed(List<byte> bytes, PropertyInfo property, string digits)
        {
            // Pad to even number of digits
            if (digits.Length % 2 != 0)
                digits += '0';

            int bcdLength = digits.Length / 2;
            if (bcdLength > 255)
                throw new InvalidOperationException(
                    $"Property '{property.Name}' BCD byte count {bcdLength} exceeds 1-byte prefix max (255).");

            bytes.Add((byte)bcdLength);
            WriteBCDStringFixed(bytes, digits, bcdLength);
        }

        private static string ReadBCDString(ReadOnlySpan<byte> bytes, ref int offset, PropertyInfo property, int fixedLength)
        {
            if (offset + fixedLength > bytes.Length)
                throw new InvalidOperationException(
                    $"Not enough bytes to read BCD string '{property.Name}' (need {fixedLength}, have {bytes.Length - offset})");

            var sb = new StringBuilder(fixedLength * 2);
            for (int i = 0; i < fixedLength; i++)
            {
                byte b = bytes[offset++];
                sb.Append((b >> 4) & 0x0F);
                sb.Append(b & 0x0F);
            }

            return sb.ToString().TrimEnd('0');
        }

        private static int ReadLengthPrefix1(ReadOnlySpan<byte> bytes, ref int offset, PropertyInfo property)
        {
            if (offset >= bytes.Length)
                throw new InvalidOperationException($"Not enough bytes to read length prefix for '{property.Name}'");
            return bytes[offset++];
        }

        private static int ReadLengthPrefix2(ReadOnlySpan<byte> bytes, ref int offset, PropertyInfo property)
        {
            if (offset + 1 >= bytes.Length)
                throw new InvalidOperationException($"Not enough bytes to read 2-byte length prefix for '{property.Name}'");
            var length = (bytes[offset] << 8) | bytes[offset + 1];
            offset += 2;
            return length;
        }
    }
}
