namespace ShoppingChallenge.Discounts.Seasonal;

/// <summary>A discount rate that applies within an annually recurring calendar window (days inclusive).</summary>
public sealed class SeasonalTier
{
    public int Month { get; }
    public int FromDay { get; }
    public int ThroughDay { get; }
    public decimal Rate { get; }

    public SeasonalTier(int month, int fromDay, int throughDay, decimal rate)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        if (fromDay < 1 || throughDay > 31 || fromDay > throughDay)
            throw new ArgumentException("Invalid day range.");
        if (rate is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "Rate must be a fraction greater than 0 and at most 1.");

        Month = month;
        FromDay = fromDay;
        ThroughDay = throughDay;
        Rate = rate;
    }

    public bool Contains(DateTime date)
        => date.Month == Month && date.Day >= FromDay && date.Day <= ThroughDay;
}
