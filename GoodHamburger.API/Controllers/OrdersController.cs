using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using GoodHamburger.Application.Orders.Commands;
using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Application.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GoodHamburger.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [TranslateResultToActionResult]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<Result<CreateOrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        => await _mediator.Send(new CreateOrderCommand(request.MenuItemIds), cancellationToken);

    [HttpGet]
    [TranslateResultToActionResult]
    [ProducesResponseType(typeof(GetOrdersResponse), StatusCodes.Status200OK)]
    public async Task<Result<GetOrdersResponse>> GetAll(CancellationToken cancellationToken)
        => await _mediator.Send(new GetOrdersQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [TranslateResultToActionResult]
    [ProducesResponseType(typeof(GetOrderByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Result<GetOrderByIdResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);

    [HttpPut("{id:guid}")]
    [TranslateResultToActionResult]
    [ProducesResponseType(typeof(UpdateOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<Result<UpdateOrderResponse>> Update(Guid id, [FromBody] UpdateOrderRequest request, CancellationToken cancellationToken)
        => await _mediator.Send(new UpdateOrderCommand(id, request.MenuItemIds), cancellationToken);

    [HttpDelete("{id:guid}")]
    [TranslateResultToActionResult]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
        => await _mediator.Send(new DeleteOrderCommand(id), cancellationToken);
}
