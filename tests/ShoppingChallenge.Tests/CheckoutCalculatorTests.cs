using ShoppingChallenge.Discounts;
using ShoppingChallenge.Enums;
using ShoppingChallenge.Models;

namespace ShoppingChallenge.Tests;

public class CheckoutCalculatorTests
{
    private sealed class FlatRule : IDiscountRule
    {
        private readonly decimal _rate;
        public FlatRule(decimal rate) => _rate = rate;
        public decimal GetDiscountRate(CartItem item, DateTime checkoutTime) => _rate;
    }

    // NOTE: these fixtures mirror the demo carts in Program.cs — keep in sync when changing scenarios.
    private static List<CartItem> ChristmasCart() => new()
    {
        CartItem.ByQuantity("Lights",    ProductCategory.Christmas, 5.99m, 10),
        CartItem.ByQuantity("Tree",      ProductCategory.Christmas, 169m,   1),
        CartItem.ByQuantity("Ornaments", ProductCategory.Christmas, 8m,    15),
    };

    private static List<CartItem> FoodCart() => new()
    {
        CartItem.ByWeight("Apple",       ProductCategory.Food, 3.27m,  0.79m),
        CartItem.ByWeight("Scallop",     ProductCategory.Food, 18m,    1.5m),
        CartItem.ByQuantity("Salad",     ProductCategory.Food, 6.99m,  1),
        CartItem.ByWeight("Ground Beef", ProductCategory.Food, 7.99m,  1.5m),
        CartItem.ByQuantity("Red Wine",  ProductCategory.Food, 25.99m, 1),
    };

    // The four scenario totals asserted EXACTLY as the original program computed them
    // (including fractional cents) — this refactor is behavior-preserving per the spec.
    [Fact]
    public void ChristmasCart_BeforeSeason_FullPrice()
        => Assert.Equal(348.90m, StorePolicy.CreateCalculator().Calculate(ChristmasCart(), new DateTime(2020, 11, 30)));

    [Fact]
    public void ChristmasCart_AfterChristmas_NinetyPercentOff()
        => Assert.Equal(34.890m, StorePolicy.CreateCalculator().Calculate(ChristmasCart(), new DateTime(2020, 12, 30)));

    [Fact]
    public void FoodCart_NoDiscount()
        => Assert.Equal(74.5483m, StorePolicy.CreateCalculator().Calculate(FoodCart(), new DateTime(2020, 11, 30)));

    [Fact]
    public void FoodCart_SeniorHour_TenPercentOff()
        => Assert.Equal(67.09347m, StorePolicy.CreateCalculator().Calculate(FoodCart(), new DateTime(2020, 11, 30, 7, 11, 0)));

    [Fact]
    public void BestSingleDiscountWins_NoStacking()
    {
        var cart = new List<CartItem> { CartItem.ByQuantity("X", ProductCategory.Food, 10m, 1) };
        var calculator = new CheckoutCalculator(new[] { new FlatRule(0.10m), new FlatRule(0.15m) });

        Assert.Equal(8.50m, calculator.Calculate(cart, new DateTime(2020, 11, 30)));   // 15%, not 25%
    }

    [Fact]
    public void FullPrecision_IsPreserved()
    {
        // 3.27 * 0.79 = 2.5833, exactly as the original computed it — no rounding in
        // the engine; converting to legal tender is the caller's concern.
        var cart = new List<CartItem> { CartItem.ByWeight("Apple", ProductCategory.Food, 3.27m, 0.79m) };
        Assert.Equal(2.5833m, new CheckoutCalculator(Array.Empty<IDiscountRule>()).Calculate(cart, new DateTime(2020, 11, 30)));
    }

    [Fact]
    public void NullCart_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StorePolicy.CreateCalculator().Calculate(null!, new DateTime(2020, 11, 30)));
    }

    [Fact]
    public void NullItemInCart_Throws()
    {
        var cart = new List<CartItem> { null! };
        Assert.Throws<ArgumentException>(() =>
            StorePolicy.CreateCalculator().Calculate(cart, new DateTime(2020, 11, 30)));
    }
}
