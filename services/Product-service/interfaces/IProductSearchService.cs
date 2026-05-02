using product_service.DTOs;

namespace product_service.Interfaces;

public interface IProductSearchService
{
    Task<(IEnumerable<ProductResponseDto> Products, long TotalCount)> SearchAsync(string query, int page, int pageSize);
}
