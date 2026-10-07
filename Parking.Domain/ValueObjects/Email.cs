using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parking.Domain.ValueObjects
{
    public sealed class Email : IEquatable<Email>
    {
        private static readonly Regex Pattern =
                    new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        public string Value { get; }
        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email is required", nameof(value));

            var normalized = value.Trim().ToLowerInvariant();

            if (!Pattern.IsMatch(normalized))
                throw new ArgumentException("Invalid email format", nameof(value));

            Value = normalized;
        }
        public override string ToString() => Value;

        public bool Equals(Email? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => Equals(obj as Email);
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Email? left, Email? right) => Equals(left, right);
        public static bool operator !=(Email? left, Email? right) => !Equals(left, right);

        public static implicit operator string(Email email) => email.Value;
    }
}
