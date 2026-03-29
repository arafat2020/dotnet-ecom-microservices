using Microsoft.AspNetCore.Mvc;
using product_service.DTOs;
using product_service.Interfaces;

namespace product_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController: ControllerBase
{
    private readonly ICategoryService _category;

    public CategoryController(ICategoryService category)
    {
        _category = category;
    }


    [HttpPost]
    public async Task<IActionResult> create(CreateCategoryDto dto)
    {
        var category = await _category.CreateAsync(dto);
        return Ok(category);
    }

    
    [HttpGet]
    public async Task<IActionResult> getAll()
    {
        var categories = await _category.GetAllAsync();
        return Ok(categories);
    }


}