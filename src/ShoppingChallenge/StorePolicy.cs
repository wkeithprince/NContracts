using ShoppingChallenge.Discounts;
using ShoppingChallenge.Discounts.Seasonal;
using ShoppingChallenge.Discounts.TimeOfDay;
using ShoppingChallenge.Enums;

namespace ShoppingChallenge;

/// <summary>
/// Composition root — the store's entire discount policy in one place, as data.
///
/// To extend an existing sale (another month, a changed rate): edit the tier/window
/// data below. To add a brand-new kind of discount: write one class implementing
/// IDiscountRule and add it to this list. Nothing else in the codebase changes.
/// </summary>
public static class StorePolicy
{
    public static CheckoutCalculator CreateCalculator() => new(new IDiscountRule[]
    {
        // Christmas markdown: Dec 1-14 -> 20%, Dec 15-25 -> 60%, Dec 26-31 -> 90%.
        new SeasonalCategoryDiscountRule(ProductCategory.Christmas, new[]
        {
            new SeasonalTier(month: 12, fromDay: 1,  throughDay: 14, rate: 0.20m),
            new SeasonalTier(month: 12, fromDay: 15, throughDay: 25, rate: 0.60m),
            new SeasonalTier(month: 12, fromDay: 26, throughDay: 31, rate: 0.90m),
        }),

        // Senior hour: food 10% off, 07:00:00-08:59:59 (preserved from the original code).
        new TimeOfDayCategoryDiscountRule(ProductCategory.Food,
            startInclusive: new TimeSpan(7, 0, 0),
            endExclusive: new TimeSpan(9, 0, 0),
            rate: 0.10m),
    });
}
