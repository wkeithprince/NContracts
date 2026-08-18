using ShoppingChallenge.Enums;
using ShoppingChallenge.Models;

namespace ShoppingChallenge.Discounts.TimeOfDay;

/// <summary>Flat discount for a category during a daily time window [start, end).</summary>
public sealed class TimeOfDayCategoryDiscountRule : IDiscountRule
{
    private readonly ProductCategory _category;
    private readonly TimeSpan _startInclusive;
    private readonly TimeSpan _endExclusive;
    private readonly decimal _rate;

    public TimeOfDayCategoryDiscountRule(ProductCategory category,
        TimeSpan startInclusive, TimeSpan endExclusive, decimal rate)
    {
        if (rate is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "Rate must be a fraction greater than 0 and at most 1.");
        if (startInclusive >= endExclusive)
            throw new ArgumentException("Window start must precede its end.");

        _category = category;
        _startInclusive = startInclusive;
        _endExclusive = endExclusive;
        _rate = rate;
    }

    public decimal GetDiscountRate(CartItem item, DateTime checkoutTime)
    {
        if (item.Category != _category)
            return 0m;

        var timeOfDay = checkoutTime.TimeOfDay;
        return timeOfDay >= _startInclusive && timeOfDay < _endExclusive ? _rate : 0m;
    }
}
