using Shared.Protos.Product;
using product_service.DTOs;
using product_service.Interfaces;
using Grpc.Core;

namespace Product_service.GrpcServices;

public class ProductGrpcService: ProductService.ProductServiceBase
{
    private readonly IProductService _productService;

    public ProductGrpcService(IProductService productService)
    {
        _productService = productService;
    }

    public override async Task<ProductResponse> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
       var dto = new CreateProductDto
       {
            Name = request.Name,
            Description = request.Description,
            BasePrice = (decimal)request.BasePrice,
            CategoryId = Guid.Parse(request.CategoryId)
       };

       var result = await _productService.CreateAsync(dto);

         return new ProductResponse
         {
                Id = result.Id.ToString(),
                Name = result.Name,
                Description = result.Description,
                BasePrice = (double)result.BasePrice,
                CategoryId = result.CategoryId.ToString()
         };
    }
}
