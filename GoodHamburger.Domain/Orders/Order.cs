using Ardalis.GuardClauses;
using Ardalis.Result;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders.Errors;

namespace GoodHamburger.Domain.Orders;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }

    private Order() { }

    public static Result<Order> Create(List<MenuItem> menuItems)
    {
        if (menuItems == null || menuItems.Count == 0)
            return Result.Invalid(new ValidationError(OrderErrors.EmptyOrder));

        var order = new Order();

        foreach (var item in menuItems)
        {
            var result = order.AddItem(item);
            if (!result.IsSuccess)
                return Result.Invalid(result.ValidationErrors.ToArray());
        }

        return Result.Success(order);
    }

    public Result ReplaceItems(List<MenuItem> menuItems)
    {
        _items.Clear();

        foreach (var item in menuItems)
        {
            var result = AddItem(item);
            if (!result.IsSuccess)
                return result;
        }

        Recalculate();
        SetUpdated();
        return Result.Success();
    }

    private Result AddItem(MenuItem item)
    {
        Guard.Against.Null(item);

        if (_items.Any(i => i.Category == item.Category))
            return Result.Invalid(new ValidationError(
                string.Format(OrderErrors.DuplicateCategory, item.Category)));

        _items.Add(new OrderItem(item));
        Recalculate();
        SetUpdated();
        return Result.Success();
    }

    private void Recalculate()
    {
        Subtotal = _items.Sum(i => i.Price);

        bool hasSandwich = _items.Any(i => i.Category == ItemCategory.Sandwich);
        bool hasSide = _items.Any(i => i.Category == ItemCategory.Side);
        bool hasDrink = _items.Any(i => i.Category == ItemCategory.Drink);

        decimal discountRate = (hasSandwich, hasSide, hasDrink) switch
        {
            (true, true, true) => 0.20m,
            (true, false, true) => 0.15m,
            (true, true, false) => 0.10m,
            _ => 0m
        };

        Discount = Subtotal * discountRate;
        Total = Subtotal - Discount;
    }
}
