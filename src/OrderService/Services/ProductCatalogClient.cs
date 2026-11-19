using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Services;

public sealed record ProductSnapshot(int Id, string Name, decimal Price, int Quantity);

public interface IProductCatalogClient
{
    Task<ProductSnapshot?> GetProductAsync(int productId, string? bearerToken, CancellationToken ct);
}

public sealed class ProductCatalogClient(HttpClient httpClient) : IProductCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProductSnapshot?> GetProductAsync(int productId, string? bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"products/{productId}");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductSnapshot>(JsonOptions, ct)
               ?? throw new InvalidOperationException("ProductService trả về dữ liệu sản phẩm rỗng.");
    }
}
