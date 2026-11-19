using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportService.Contracts;
using ReportService.Services;

namespace ReportService.Controllers;

[ApiController]
[Route("reports")]
[Authorize]
[Produces("application/json")]
public sealed class ReportsController(ReportAppService reports) : ControllerBase
{
    [HttpGet("products")]
    [ProducesResponseType(typeof(ProductReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ProductReportResponse>> GetProducts([FromQuery] ReportQuery query, CancellationToken ct)
    {
        try { return Ok(await reports.GetProductsAsync(GetToken(), query, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SourceServiceException ex) { return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message }); }
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(OrderReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<OrderReportResponse>> GetOrders([FromQuery] ReportQuery query, CancellationToken ct)
    {
        try { return Ok(await reports.GetOrdersAsync(GetToken(), query, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SourceServiceException ex) { return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message }); }
    }

    [HttpPost("products/snapshot")]
    public async Task<ActionResult<SavedReportResponse>> SaveProductSnapshot([FromQuery] ReportQuery query, CancellationToken ct)
    {
        try
        {
            var report = await reports.GetProductsAsync(GetToken(), query, ct);
            return Ok(await reports.SaveProductReportAsync(report, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SourceServiceException ex) { return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message }); }
    }

    [HttpPost("orders/snapshot")]
    public async Task<ActionResult<SavedReportResponse>> SaveOrderSnapshot([FromQuery] ReportQuery query, CancellationToken ct)
    {
        try
        {
            var report = await reports.GetOrdersAsync(GetToken(), query, ct);
            return Ok(await reports.SaveOrderReportAsync(report, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SourceServiceException ex) { return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message }); }
    }

    [HttpGet("snapshots")]
    public async Task<ActionResult<IReadOnlyList<SavedReportResponse>>> GetSnapshots(CancellationToken ct) =>
        Ok(await reports.GetSavedReportsAsync(ct));

    private string GetToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
    }
}
