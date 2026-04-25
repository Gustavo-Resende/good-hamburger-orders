using GoodHamburger.Application.Interfaces;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Infrastructure.Seed;

namespace GoodHamburger.Infrastructure.Services;

public class MenuService : IMenuService
{
    private readonly List<MenuItem> _items = MenuSeed.Items;

    public MenuItem? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);
    public List<MenuItem> GetAll() => _items;
}
