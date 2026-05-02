using Shared.Protos.Product;
using product_service.DTOs;
using product_service.Interfaces;
using Grpc.Core;

namespace Product_service.GrpcServices;

public class ProductGrpcService: ProductService.ProductServiceBase
{
    private readonly IProductService _productService;
    private readonly IProductSearchService _productSearchService;

    public ProductGrpcService(IProductService productService, IProductSearchService productSearchService)
    {
        _productService = productService;
        _productSearchService = productSearchService;
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

    public override async Task<SearchProductsResponse> SearchProducts(SearchProductsRequest request, ServerCallContext context)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var (products, totalCount) = await _productSearchService.SearchAsync(request.Query, page, pageSize);

        var response = new SearchProductsResponse
        {
            TotalCount = totalCount
        };

        foreach (var p in products)
        {
            response.Products.Add(new ProductResponse
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Description = p.Description,
                BasePrice = (double)p.BasePrice,
                CategoryId = p.CategoryId.ToString()
            });
        }

        return response;
    }
}
