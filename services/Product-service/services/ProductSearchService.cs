using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.EntityFrameworkCore;
using product_service.DTOs;
using product_service.Interfaces;

namespace product_service.services;

public class ProductSearchService : IProductSearchService
{
    private readonly ElasticsearchClient _elasticsearchClient;
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(
        ElasticsearchClient elasticsearchClient,
        ProductDbContext context,
        ILogger<ProductSearchService> logger)
    {
        _elasticsearchClient = elasticsearchClient;
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<ProductResponseDto> Products, long TotalCount)> SearchAsync(string query, int page, int pageSize)
    {
        try
        {
            var response = await _elasticsearchClient.SearchAsync<ProductResponseDto>(s => s
                .Index("products")
                .From((page - 1) * pageSize)
                .Size(pageSize)
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query)
                        .Fields(new[] { "name^3", "description" }) // Boost name relevance
                    )
                )
            );

            if (response.IsValidResponse)
            {
                var products = response.Documents;
                var total = response.Total;
                return (products, total);
            }

            _logger.LogWarning("Elasticsearch search was not valid. Falling back to Database search. Reason: {Reason}", response.DebugInformation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch is down or an error occurred during search. Falling back to Database search.");
        }

        // Fallback to Database
        var dbQuery = _context.products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            dbQuery = dbQuery.Where(p => p.Name.ToLower().Contains(lowerQuery) || p.Description.ToLower().Contains(lowerQuery));
        }

        var totalCount = await dbQuery.CountAsync();

        var dbProducts = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                BasePrice = p.BasePrice,
                CategoryId = p.CategoryId
            })
            .ToListAsync();

        return (dbProducts, totalCount);
    }
}
