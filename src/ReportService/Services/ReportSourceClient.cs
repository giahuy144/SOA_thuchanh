using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReportService.Services;

public sealed record ProductDto(int Id, string Name, string? Description, decimal Price, int Quantity,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
public sealed record OrderDto(int Id, string CustomerName, string Status, decimal TotalAmount,
    DateTime CreatedAt, DateTime UpdatedAt, List<OrderItemDto> Items);

public sealed class SourceServiceException(string message, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

public interface IReportSourceClient
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(string token, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(string token, CancellationToken ct);
}

public sealed class ReportSourceClient(IHttpClientFactory clients) : IReportSourceClient
{
    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(string token, CancellationToken ct) =>
        GetListAsync<ProductDto>(clients.CreateClient("ProductService"), "products", token, "ProductService", ct);

    public Task<IReadOnlyList<OrderDto>> GetOrdersAsync(string token, CancellationToken ct) =>
        GetListAsync<OrderDto>(clients.CreateClient("OrderService"), "orders", token, "OrderService", ct);

    private static async Task<IReadOnlyList<T>> GetListAsync<T>(HttpClient client, string path, string token,
        string serviceName, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                throw new SourceServiceException(
                    $"{serviceName} trả về HTTP {(int)response.StatusCode}; không thể tạo báo cáo.", response.StatusCode);

            return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct)
                   ?? throw new SourceServiceException($"{serviceName} trả về danh sách rỗng không hợp lệ.");
        }
        catch (HttpRequestException ex)
        {
            throw new SourceServiceException($"Không thể kết nối {serviceName}.", null, ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new SourceServiceException($"{serviceName} phản hồi quá thời gian cho phép.", null, ex);
        }
    }
}
