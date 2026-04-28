using product_service.DTOs;

namespace product_service.Interfaces;
public interface IProductService
{
    Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
}