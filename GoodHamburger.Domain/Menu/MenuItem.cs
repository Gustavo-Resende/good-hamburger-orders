using GoodHamburger.Domain.Enums;

namespace GoodHamburger.Domain.Menu;

public class MenuItem
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public decimal Price { get; init; }
    public ItemCategory Category { get; init; }

    public MenuItem(Guid id, string name, decimal price, ItemCategory category)
    {
        Id = id;
        Name = name;
        Price = price;
        Category = category;
    }
}
