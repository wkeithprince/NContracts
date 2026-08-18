using ShoppingChallenge.Models;

namespace ShoppingChallenge.Discounts;

/// <summary>
/// The extension point for all discount logic. A rule answers one question: what
/// discount fraction (0..1) do I grant this item at this checkout time? 0 = "I don't
/// apply". Adding a new kind of discount = one new implementation, registered in
/// StorePolicy — the calculator and existing rules are never edited.
///
/// checkoutTime is store-local wall-clock time; all rule windows (seasonal days,
/// time-of-day) are interpreted in that same local clock. No timezone conversion
/// happens anywhere in this codebase — multi-region callers must convert first.
/// </summary>
public interface IDiscountRule
{
    decimal GetDiscountRate(CartItem item, DateTime checkoutTime);
}
