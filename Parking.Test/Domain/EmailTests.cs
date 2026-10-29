using FluentAssertions;
using Parking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Domain
{
    public sealed class EmailTests
    {
        [Fact]
        public void Constructor_NormalizesToTrimmedLowerCase()
        {
            new Email("  User@Example.COM ").Value.Should().Be("user@example.com");
        }
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("a@b")]
        [InlineData("a b@c.com")]
        public void Constructor_InvalidEmail_ThrowsArgumentException(string value)
        {
            Action act = () => new Email(value);

            act.Should().Throw<ArgumentException>();
        }
        [Fact]
        public void Equality_DifferentCasing_AreEqual()
        {
            new Email("A@b.com").Should().Be(new Email("a@B.com"));
        }
    }
}
