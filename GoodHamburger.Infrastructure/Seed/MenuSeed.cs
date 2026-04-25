using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;

namespace GoodHamburger.Infrastructure.Seed;

public static class MenuSeed
{
    public static readonly List<MenuItem> Items =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "X-Burger",      5.00m, ItemCategory.Sandwich),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "X-Egg",         4.50m, ItemCategory.Sandwich),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "X-Bacon",       7.00m, ItemCategory.Sandwich),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Batata Frita",  2.00m, ItemCategory.Side),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Refrigerante",  2.50m, ItemCategory.Drink),
    ];
}
