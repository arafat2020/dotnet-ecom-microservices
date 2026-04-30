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
            CategoryId = Guid.Parse(request.CategoryId),
            Variants = request.Variants.Select(v => new CreateProductVariantDto
            {
                Name = v.Name,
                Price = v.Price == 0 ? null : (decimal)v.Price,
                Stock = v.Stock,
                SKU = v.Sku
            }).ToList(),
            Images = request.Images.Select(i => new CreateProductImageDto
            {
                ImageFileId = Guid.Parse(i.ImageFileId),
                Url = i.Url,
                AltText = i.AltText,
                IsPrimary = i.IsPrimary
            }).ToList()
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
