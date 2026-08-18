# RxSense

Grocery checkout calculator — a behavior-preserving refactor of a tightly-coupled
coding challenge into an extensible discount engine.

## The Challenge

The original file ([`docs/original/ShoppingChallenge.cs`](docs/original/ShoppingChallenge.cs))
states the problem in its header, quoted verbatim:

> *"This code requires refactoring to accomadate expnding discounts to other months or
> introducing various discounts. As of now, the code is tightly coupled and making
> changes is difficult. Goal of the refactor is for more robust, modular and decoupled
> code for extending capablites as previously mentioned."*

In short: **restructure the code so discounts can be extended to other months and new
kinds of discounts can be added — without the current pain.** The existing results are
treated as accurate, so the refactor must preserve them.

## Results — Original vs. This Solution

Both programs run the same four scenarios:

| Scenario | Original output | This solution | Match |
|---|---|---|---|
| Christmas cart, Nov 30 (no discount) | `348.90` | `348.90` | ✅ identical |
| Christmas cart, Dec 30 (90% clearance) | `34.890` | `34.8900` | ✅ identical value |
| Food cart, Nov 30 (no discount) | `74.5483` | `74.5483` | ✅ identical |
| Food cart, 7:11 AM (senior hour, 10% off) | `67.09347` | `67.093470` | ✅ identical value |

Two notes on that table:

- **The trailing zeros are formatting, not value.** C#'s `decimal` remembers how many
  decimal places the arithmetic produced, and the refactored formula takes an
  algebraically equivalent but differently-ordered path. `34.890` and `34.8900` are the
  same number — the unit tests assert exact value equality
  (`Assert.Equal(34.890m, ...)` passes) to prove it.
- **Full precision is deliberately preserved.** The spec treats the original results as
  accurate, so the engine does not round `74.5483` to `74.55`. Converting a computed
  total into legal tender is a concern for the layer that charges the customer or
  prints the receipt — and the calculator documents the single line where rounding
  would go if the business ever decides otherwise.

## What Changed and Why

The original computed everything inside one 60-line method of nested `if/else`
branches. Five changes, each solving a specific problem it caused:

**1. Nested conditionals → one small class per discount (the core change).**
Adding any discount to the original meant editing the same fragile method that
contained every *other* discount — the exact pain the header describes. Now each
discount is its own class implementing a one-method interface (`IDiscountRule`), and
the calculator simply asks every registered rule for its rate. Rules don't know about
each other, so adding one cannot break another. This is the Strategy pattern — chosen
because it is the *smallest* structure that turns "add a discount" from surgery on
shared code into an additive change, which is precisely what the spec asks for.

**2. Magic strings → a `ProductCategory` enum.**
The original compared categories as raw strings, so a typo'd `"christmas"` silently
charged full price, and an empty category hit a branch commented `//oh no! this should
not happen!` that priced items at $0. With an enum, the compiler rejects a category
that doesn't exist — that whole class of bug is gone rather than guarded against.

**3. Ambiguous items → factory-built `CartItem`.**
The original `CartItem` allowed contradictions: an item with both a quantity and a
weight, or neither (which rang up **free**). Items are now immutable and built through
`ByQuantity(...)` or `ByWeight(...)`, which validate their inputs — invalid items can no
longer be constructed, so downstream code never has to guess.

**4. Scattered numbers → one policy file.**
Every rate, date boundary, and time window was buried in expressions like
`item.Price - item.Price * (20m / 100m)` and `Hours > 6 && Hours <= 8`. All of it now
lives in `StorePolicy` as named, readable data — the store's entire discount policy in
one block. Extending a sale to another month is a one-line edit there.

**5. No tests → 34 tests, boundaries first.**
The original's riskiest spots were its date and time edges (`Day < 15` vs `Day <= 25`,
`Hours > 6`). Tests now pin the exact behavior on both sides of every edge (Dec 14/15,
Dec 25/26, 6:59:59/7:00:00, 8:59:59/9:00:00), and the four scenario totals above are
asserted to the exact original values — proof the refactor changed *structure*, not
*behavior*.

Decisions worth stating explicitly: when multiple discounts apply to one item, the
single largest wins (no stacking — the original never combined discounts, and now that
policy is written down and tested rather than being an accident of branch order). And
the senior-hour window is `07:00:00–08:59:59` — exactly what the original's
`Hours > 6 && Hours <= 8` computed, preserved on purpose.

## Project Structure

```
src/ShoppingChallenge/
├── Enums/
│   └── ProductCategory.cs            product categories (no magic strings)
├── Models/
│   └── CartItem.cs                   immutable line item; factory-validated
├── Discounts/
│   ├── IDiscountRule.cs              the extension point (one method)
│   ├── Seasonal/
│   │   ├── SeasonalTier.cs           one calendar window + rate
│   │   └── SeasonalCategoryDiscountRule.cs
│   └── TimeOfDay/
│       └── TimeOfDayCategoryDiscountRule.cs
├── CheckoutCalculator.cs             totals a cart; best single discount wins
├── StorePolicy.cs                    composition root — the whole policy, as data
└── Program.cs                        the original demo scenarios
tests/ShoppingChallenge.Tests/        34 xUnit tests (boundaries + exact totals)
docs/original/ShoppingChallenge.cs    the unmodified original, for comparison
```

## How to Extend

**Extend an existing sale** (another month, a changed rate) — one line of data in
`StorePolicy`:

```csharp
new SeasonalTier(month: 11, fromDay: 20, throughDay: 30, rate: 0.10m),
```

**Add a brand-new kind of discount** — one class, one registration:

```csharp
public sealed class LoyaltyDiscountRule : IDiscountRule
{
    public decimal GetDiscountRate(CartItem item, DateTime checkoutTime)
        => /* your condition */ ? 0.05m : 0m;
}
```

then add it to the list in `StorePolicy`. The calculator and every existing rule are
untouched — you cannot break what you didn't edit.

## How to Run

```
dotnet run --project src/ShoppingChallenge      # prints the four scenario totals
dotnet test                                     # runs the 34-test suite
```

## Future Improvements

Deliberately not built — the spec asks for extensible code, not these features — but
the design leaves a clean seam for each:

**Database-backed discount policy.** Today, changing a rate means editing `StorePolicy`
and redeploying. Moving that data behind a repository interface (e.g.
`IDiscountPolicySource` reading `DiscountRules` / `SeasonalTiers` tables) would let
rates, dates, and windows change without a deployment, and unlocks what a real store
eventually needs: an admin screen for non-developers, an audit trail of who changed
which rate when, and effective-dating ("this goes live Friday midnight"). Only
`StorePolicy` — the composition root — changes; the rules and calculator already
receive their configuration as constructor data, so they wouldn't know the difference.
The one design point to get right: cache the loaded policy and rebuild only when it
changes, so pricing never pays a database read per checkout.

**Promo codes.** Codes are best modeled as *data, not code*: a `PromoCodeDefinition`
(code, rate, optional validity window, optional category filter) in a repository, served
by a single `PromoCodeDiscountRule` that looks up whatever codes the customer entered.
Launching a new code is then a data entry, not a deployment. This needs one widening of
the rule interface — the `DateTime` parameter becomes a `CheckoutContext` carrying the
time *and* the entered codes — done once, after which promo codes plug into the same
best-discount-wins calculation as every other rule. Two follow-on considerations:
matching should be case-insensitive, and a typo'd or expired code should produce
feedback for the cashier rather than silently doing nothing — which means `Calculate`
eventually returning a small result object (total + notes) instead of a bare `decimal`.

Both improvements attach at seams the current design already has (`StorePolicy` for
storage, `IDiscountRule` for new behavior). That's the measure of the refactor: the
next features are additions, not rewrites.
