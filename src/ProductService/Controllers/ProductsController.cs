using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Contracts;
using ProductService.Services;

namespace ProductService.Controllers;

/// <summary>
/// RESTful API quản lý sản phẩm.
/// Mọi endpoint đều yêu cầu JWT hợp lệ do LoginService (bài thực hành số 2) cấp.
/// </summary>
[ApiController]
[Route("products")]
[Authorize]
[Produces("application/json")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    /// <summary>Lấy danh sách tất cả sản phẩm.</summary>
    /// <response code="200">Trả về mảng sản phẩm.</response>
    /// <response code="401">Thiếu hoặc sai JWT.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll(CancellationToken ct)
    {
        var items = await products.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>Lấy thông tin chi tiết một sản phẩm theo id.</summary>
    /// <response code="200">Tìm thấy sản phẩm.</response>
    /// <response code="404">Không có sản phẩm với id này.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id, ct);
        return product is null
            ? NotFound(new { message = $"Không tìm thấy sản phẩm có id = {id}." })
            : Ok(product);
    }

    /// <summary>Thêm một sản phẩm mới.</summary>
    /// <response code="201">Tạo thành công, trả về sản phẩm kèm header Location.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request, CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật thông tin sản phẩm.</summary>
    /// <response code="200">Cập nhật thành công.</response>
    /// <response code="404">Không có sản phẩm với id này.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] ProductRequest request, CancellationToken ct)
    {
        var updated = await products.UpdateAsync(id, request, ct);
        return updated is null
            ? NotFound(new { message = $"Không tìm thấy sản phẩm có id = {id}." })
            : Ok(updated);
    }

    /// <summary>Xóa một sản phẩm.</summary>
    /// <response code="204">Xóa thành công.</response>
    /// <response code="404">Không có sản phẩm với id này.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await products.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : NotFound(new { message = $"Không tìm thấy sản phẩm có id = {id}." });
    }
}
