using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Controllers;

[ApiController]
[Authorize] // mọi API đều yêu cầu JWT hợp lệ do AuthenticationService cấp
[Route("products")]
public class ProductsController(ProductDbContext db) : ControllerBase
{
    // GET /products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        => Ok(await db.Products.AsNoTracking().OrderByDescending(p => p.Id).ToListAsync());

    // GET /products/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound(new { message = "Không tìm thấy sản phẩm" }) : Ok(product);
    }

    // POST /products
    [HttpPost]
    public async Task<ActionResult<Product>> Create(ProductRequest req)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            Quantity = req.Quantity,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /products/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Product>> Update(int id, ProductRequest req)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

        product.Name = req.Name;
        product.Description = req.Description;
        product.Price = req.Price;
        product.Quantity = req.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(product);
    }

    // DELETE /products/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa sản phẩm" });
    }
}