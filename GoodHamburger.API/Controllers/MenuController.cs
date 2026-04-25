using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Application.Orders.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GoodHamburger.API.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService) => _menuService = menuService;

    [HttpGet]
    [ProducesResponseType(typeof(GetMenuResponse), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var items = _menuService.GetAll().Select(i => i.ToResponse()).ToList();
        return Ok(new GetMenuResponse(items));
    }
}
