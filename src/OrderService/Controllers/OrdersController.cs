using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Contracts;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController(OrderAppService orders) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken ct) =>
        Ok(await orders.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct);
        return order is null ? NotFound(new { message = $"Không tìm thấy đơn hàng id={id}." }) : Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var bearerToken = Request.Headers.Authorization.ToString();
        if (bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            bearerToken = bearerToken[7..].Trim();
        else
            bearerToken = string.Empty;

        try
        {
            var (order, error) = await orders.CreateAsync(request, bearerToken, ct);
            if (error is not null) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetById), new { id = order!.Id }, order);
        }
        catch (ProductServiceUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (!OrderStatuses.All.Contains(request.Status.Trim()))
            return BadRequest(new { message = $"Trạng thái hợp lệ: {string.Join(", ", OrderStatuses.All.Order())}." });

        var updated = await orders.UpdateStatusAsync(id, request.Status.Trim(), ct);
        return updated is null ? NotFound(new { message = $"Không tìm thấy đơn hàng id={id}." }) : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await orders.DeleteAsync(id, ct)
            ? NoContent()
            : NotFound(new { message = $"Không tìm thấy đơn hàng id={id}." });
}
