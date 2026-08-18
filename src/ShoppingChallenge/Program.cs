using ShoppingChallenge.Enums;
using ShoppingChallenge.Models;

namespace ShoppingChallenge;

public static class Program
{
    public static void Main()
    {
        ChristmasShoppingAtTheGroceryStore();
        BuyingFood();
    }

    // NOTE: both demo carts are mirrored by the fixtures in CheckoutCalculatorTests —
    // keep them in sync when changing scenarios.
    static void ChristmasShoppingAtTheGroceryStore()
    {
        var cartItems = new List<CartItem>
        {
            CartItem.ByQuantity("Lights",    ProductCategory.Christmas, 5.99m, 10),
            CartItem.ByQuantity("Tree",      ProductCategory.Christmas, 169m,   1),
            CartItem.ByQuantity("Ornaments", ProductCategory.Christmas, 8m,    15),
        };

        var calculator = StorePolicy.CreateCalculator();

        Console.WriteLine(calculator.Calculate(cartItems, new DateTime(2020, 11, 30)));
        Console.WriteLine(calculator.Calculate(cartItems, new DateTime(2020, 12, 30)));
    }

    static void BuyingFood()
    {
        var cartItems = new List<CartItem>
        {
            CartItem.ByWeight("Apple",       ProductCategory.Food, 3.27m,  0.79m),
            CartItem.ByWeight("Scallop",     ProductCategory.Food, 18m,    1.5m),
            CartItem.ByQuantity("Salad",     ProductCategory.Food, 6.99m,  1),
            CartItem.ByWeight("Ground Beef", ProductCategory.Food, 7.99m,  1.5m),
            CartItem.ByQuantity("Red Wine",  ProductCategory.Food, 25.99m, 1),
        };

        var calculator = StorePolicy.CreateCalculator();

        Console.WriteLine(calculator.Calculate(cartItems, new DateTime(2020, 11, 30)));
        Console.WriteLine(calculator.Calculate(cartItems, new DateTime(2020, 11, 30, 7, 11, 0)));
    }
}
