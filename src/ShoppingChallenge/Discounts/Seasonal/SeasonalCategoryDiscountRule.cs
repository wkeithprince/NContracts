using ShoppingChallenge.Enums;
using ShoppingChallenge.Models;

namespace ShoppingChallenge.Discounts.Seasonal;

/// <summary>
/// Tiered seasonal discount for one product category. Extending a sale to another
/// month or changing a rate = editing the tier data in StorePolicy, nothing else.
/// </summary>
public sealed class SeasonalCategoryDiscountRule : IDiscountRule
{
    private readonly ProductCategory _category;
    private readonly IReadOnlyList<SeasonalTier> _tiers;

    public SeasonalCategoryDiscountRule(ProductCategory category, IReadOnlyList<SeasonalTier> tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        if (tiers.Count == 0)
            throw new ArgumentException("A seasonal rule needs at least one tier.", nameof(tiers));

        // Overlapping tiers would make "which rate applies" ambiguous (first match would
        // silently win) — rejected outright, same reasoning as empty tiers and 0 rates.
        for (var firstIndex = 0; firstIndex < tiers.Count; firstIndex++)
        for (var secondIndex = firstIndex + 1; secondIndex < tiers.Count; secondIndex++)
        {
            var firstTier = tiers[firstIndex];
            var secondTier = tiers[secondIndex];
            if (firstTier.Month == secondTier.Month
                && firstTier.FromDay <= secondTier.ThroughDay
                && secondTier.FromDay <= firstTier.ThroughDay)
            {
                throw new ArgumentException(
                    $"Overlapping tiers: month {firstTier.Month} days {firstTier.FromDay}-{firstTier.ThroughDay} and {secondTier.FromDay}-{secondTier.ThroughDay}.",
                    nameof(tiers));
            }
        }

        _category = category;
        _tiers = tiers;
    }

    public decimal GetDiscountRate(CartItem item, DateTime checkoutTime)
    {
        if (item.Category != _category)
            return 0m;

        foreach (var tier in _tiers)
        {
            if (tier.Contains(checkoutTime))
                return tier.Rate;
        }
        return 0m;
    }
}
