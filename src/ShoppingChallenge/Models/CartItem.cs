using ShoppingChallenge.Enums;

namespace ShoppingChallenge.Models;

/// <summary>
/// An immutable line item. Built through ByQuantity/ByWeight so an item always has
/// exactly one measurement mode — no more "quantity 0 and weight 0 rings up free".
/// </summary>
public sealed class CartItem
{
    public string ProductName { get; }
    public ProductCategory Category { get; }

    /// <summary>Price per unit (ByQuantity) or per unit of weight (ByWeight).</summary>
    public decimal UnitPrice { get; }

    /// <summary>Unit count or weight — fixed by the factory used.</summary>
    public decimal Amount { get; }

    /// <summary>Undiscounted price for this line.</summary>
    public decimal BasePrice => UnitPrice * Amount;

    private CartItem(string productName, ProductCategory category, decimal unitPrice, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Price cannot be negative.");
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Quantity/weight must be positive.");

        ProductName = productName;
        Category = category;
        UnitPrice = unitPrice;
        Amount = amount;
    }

    public static CartItem ByQuantity(string productName, ProductCategory category, decimal unitPrice, int quantity)
        => new(productName, category, unitPrice, quantity);

    public static CartItem ByWeight(string productName, ProductCategory category, decimal pricePerWeightUnit, decimal weight)
        => new(productName, category, pricePerWeightUnit, weight);
}
