using ShoppingChallenge.Discounts;
using ShoppingChallenge.Models;

namespace ShoppingChallenge;

/// <summary>
/// Totals a cart. For each item, every registered rule is asked for a rate and the
/// single best one is applied (no stacking).
///
/// The total preserves full decimal precision — results match the original program's
/// outputs exactly (e.g. 74.5483 for the food cart), per the original spec that those
/// results are the accurate ones. Rounding to legal tender is deliberately NOT done
/// here: it is a presentation/charging concern for the caller (receipt printer,
/// payment charge). If the business ever rules fractional cents wrong, a single
/// Math.Round on the line total below is the place it belongs.
/// </summary>
public sealed class CheckoutCalculator
{
    private readonly IReadOnlyList<IDiscountRule> _rules;

    public CheckoutCalculator(IReadOnlyList<IDiscountRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
    }

    public decimal Calculate(IEnumerable<CartItem> items, DateTime checkoutTime)
    {
        ArgumentNullException.ThrowIfNull(items);

        decimal total = 0m;
        foreach (var item in items)
        {
            if (item is null)
                throw new ArgumentException("Cart contains a null item.", nameof(items));

            var payableFraction = 1m - BestDiscountRate(item, checkoutTime); // 20% off => pay 0.80
            total += item.BasePrice * payableFraction;
        }
        return total;
    }

    // Composition policy: the single largest applicable discount wins (no stacking).
    // On a tie, the rule registered first in StorePolicy wins.
    private decimal BestDiscountRate(CartItem item, DateTime checkoutTime)
    {
        decimal bestRate = 0m;
        foreach (var rule in _rules)
        {
            var rate = rule.GetDiscountRate(item, checkoutTime);
            if (rate > bestRate)
                bestRate = rate;
        }
        return bestRate;
    }
}
