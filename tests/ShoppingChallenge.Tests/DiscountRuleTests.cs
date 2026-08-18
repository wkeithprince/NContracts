using ShoppingChallenge.Discounts.Seasonal;
using ShoppingChallenge.Discounts.TimeOfDay;
using ShoppingChallenge.Enums;
using ShoppingChallenge.Models;

namespace ShoppingChallenge.Tests;

public class CartItemTests
{
    [Fact]
    public void ByQuantity_ComputesBasePrice()
    {
        Assert.Equal(59.90m, CartItem.ByQuantity("Lights", ProductCategory.Christmas, 5.99m, 10).BasePrice);
    }

    [Fact]
    public void ByWeight_ComputesBasePrice()
    {
        Assert.Equal(2.5833m, CartItem.ByWeight("Apple", ProductCategory.Food, 3.27m, 0.79m).BasePrice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingProductName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => CartItem.ByQuantity(name!, ProductCategory.Food, 1m, 1));
    }

    [Fact]
    public void NegativePrice_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CartItem.ByQuantity("X", ProductCategory.Food, -1m, 1));
    }

    [Fact]
    public void ZeroQuantity_Throws()
    {
        // The original code silently priced quantity-0/weight-0 items at $0; now impossible.
        Assert.Throws<ArgumentOutOfRangeException>(() => CartItem.ByQuantity("X", ProductCategory.Food, 1m, 0));
    }

    [Fact]
    public void ZeroWeight_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CartItem.ByWeight("X", ProductCategory.Food, 1m, 0m));
    }
}

public class SeasonalCategoryDiscountRuleTests
{
    private static readonly SeasonalCategoryDiscountRule ChristmasRule = new(
        ProductCategory.Christmas,
        new[]
        {
            new SeasonalTier(month: 12, fromDay: 1,  throughDay: 14, rate: 0.20m),
            new SeasonalTier(month: 12, fromDay: 15, throughDay: 25, rate: 0.60m),
            new SeasonalTier(month: 12, fromDay: 26, throughDay: 31, rate: 0.90m),
        });

    private static readonly CartItem ChristmasItem = CartItem.ByQuantity("Tree", ProductCategory.Christmas, 169m, 1);

    // Tier boundaries — the off-by-one territory the original if/else chain risked.
    [Theory]
    [InlineData(11, 30, 0.00)]
    [InlineData(12, 1,  0.20)]
    [InlineData(12, 14, 0.20)]
    [InlineData(12, 15, 0.60)]
    [InlineData(12, 25, 0.60)]
    [InlineData(12, 26, 0.90)]
    [InlineData(12, 31, 0.90)]
    [InlineData(1,  1,  0.00)]
    public void TierBoundaries(int month, int day, decimal expectedRate)
    {
        Assert.Equal(expectedRate, ChristmasRule.GetDiscountRate(ChristmasItem, new DateTime(2020, month, day)));
    }

    [Fact]
    public void OtherCategory_GetsNoSeasonalDiscount()
    {
        var food = CartItem.ByQuantity("Salad", ProductCategory.Food, 6.99m, 1);
        Assert.Equal(0m, ChristmasRule.GetDiscountRate(food, new DateTime(2020, 12, 20)));
    }

    [Fact]
    public void InvalidTier_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonalTier(13, 1, 10, 0.5m));   // bad month
        Assert.Throws<ArgumentException>(() => new SeasonalTier(12, 20, 10, 0.5m));            // backwards days
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonalTier(12, 1, 10, 1.5m));   // rate > 1
        Assert.Throws<ArgumentOutOfRangeException>(() => new SeasonalTier(12, 1, 10, 0m));     // no-op rate
    }

    [Fact]
    public void EmptyTiers_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SeasonalCategoryDiscountRule(ProductCategory.Food, Array.Empty<SeasonalTier>()));
    }

    [Fact]
    public void OverlappingTiers_Throw()
    {
        // Dec 15-20 would match both tiers; which rate applies must never be ambiguous.
        Assert.Throws<ArgumentException>(() => new SeasonalCategoryDiscountRule(
            ProductCategory.Food,
            new[]
            {
                new SeasonalTier(month: 12, fromDay: 1,  throughDay: 20, rate: 0.20m),
                new SeasonalTier(month: 12, fromDay: 15, throughDay: 25, rate: 0.60m),
            }));
    }
}

public class TimeOfDayCategoryDiscountRuleTests
{
    private static readonly TimeOfDayCategoryDiscountRule SeniorHour = new(
        ProductCategory.Food, new TimeSpan(7, 0, 0), new TimeSpan(9, 0, 0), 0.10m);

    private static readonly CartItem FoodItem = CartItem.ByQuantity("Salad", ProductCategory.Food, 6.99m, 1);

    // Preserved from the original's `Hours > 6 && Hours <= 8`: 07:00:00 through 08:59:59.
    [Theory]
    [InlineData(6, 59, 59, 0.00)]
    [InlineData(7, 0, 0, 0.10)]
    [InlineData(8, 59, 59, 0.10)]
    [InlineData(9, 0, 0, 0.00)]
    public void WindowBoundaries(int hour, int minute, int second, decimal expectedRate)
    {
        var time = new DateTime(2020, 11, 30, hour, minute, second);
        Assert.Equal(expectedRate, SeniorHour.GetDiscountRate(FoodItem, time));
    }

    [Fact]
    public void OtherCategory_GetsNoTimeDiscount()
    {
        var tree = CartItem.ByQuantity("Tree", ProductCategory.Christmas, 169m, 1);
        Assert.Equal(0m, SeniorHour.GetDiscountRate(tree, new DateTime(2020, 11, 30, 7, 30, 0)));
    }

    [Fact]
    public void InvalidWindowOrRate_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TimeOfDayCategoryDiscountRule(
            ProductCategory.Food, new TimeSpan(9, 0, 0), new TimeSpan(7, 0, 0), 0.10m));   // backwards
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeOfDayCategoryDiscountRule(
            ProductCategory.Food, new TimeSpan(7, 0, 0), new TimeSpan(9, 0, 0), 0m));      // no-op rate
    }
}
