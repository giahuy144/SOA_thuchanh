using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportService.Contracts;
using ReportService.Models;
using ReportService.Services;

namespace ReportService.Controllers;

[ApiController]
[Route("reports/product-costs")]
[Authorize]
[Produces("application/json")]
public sealed class ProductCostsController(ReportAppService reports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductCost>>> GetAll(CancellationToken ct) =>
        Ok(await reports.GetProductCostsAsync(ct));

    [HttpPut("{productId:int}")]
    [ProducesResponseType(typeof(ProductCost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ProductCost>> Set(int productId, [FromBody] SetProductCostRequest request, CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
        try { return Ok(await reports.SetProductCostAsync(productId, request.UnitCost, token, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (SourceServiceException ex) { return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message }); }
    }
}
