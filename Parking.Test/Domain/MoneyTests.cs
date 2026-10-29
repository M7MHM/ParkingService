using FluentAssertions;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Domain
{
    public sealed class MoneyTests
    {
        [Fact]
        public void Constructor_NegativeAmount_ThrowsArgumentException()
        {
            Action act = () => new Money(-1m);

            act.Should().Throw<ArgumentException>();
        }
        [Fact]
        public void Constructor_NormalizesCurrencyToUpperCase()
        {
            new Money(10m, "egp").Currency.Should().Be("EGP");
        }
        [Fact]
        public void Add_DifferentCurrencies_ThrowsInvalidOperation()
        {
            Action act = () => new Money(10m, "EGP").Add(new Money(10m, "USD"));

            act.Should().Throw<InvalidOperationException>();
        }
        [Fact]
        public void Add_SameCurrency_SumsAmounts()
        {
            new Money(10m).Add(new Money(5.5m)).Amount.Should().Be(15.5m);
        }
        [Fact]
        public void Multiply_NegativeFactor_ThrowsArgumentException()
        {
            Action act = () => new Money(10m).Multiply(-1);

            act.Should().Throw<ArgumentException>();
        }
        [Fact]
        public void Equality_SameAmountAndCurrency_AreEqual()
        {
            (new Money(10m, "EGP") == new Money(10m, "egp")).Should().BeTrue();
            new Money(10m).Should().NotBe(new Money(11m));
        }
    }
}
