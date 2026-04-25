using GoodHamburger.Domain.Menu;

namespace GoodHamburger.Application.Interfaces;

public interface IMenuService
{
    MenuItem? GetById(Guid id);
    List<MenuItem> GetAll();
}
