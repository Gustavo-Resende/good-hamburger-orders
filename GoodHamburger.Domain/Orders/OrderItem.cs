using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;

namespace GoodHamburger.Domain.Orders;

public class OrderItem
{
    public Guid MenuItemId { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public ItemCategory Category { get; private set; }

    internal OrderItem(MenuItem item)
    {
        MenuItemId = item.Id;
        Name = item.Name;
        Price = item.Price;
        Category = item.Category;
    }

    private OrderItem() { Name = null!; }
}
