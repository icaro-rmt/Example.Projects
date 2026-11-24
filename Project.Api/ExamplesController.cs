using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NB.Omc.Cubo.Contracts;

namespace Project.Api;

[ApiController]
[Route("api/examples")]
public sealed class ExamplesController : ControllerBase
{
    private static readonly Product[] Products =
    [
        new(1, "Notebook"),
        new(2, "Pen"),
        new(3, "Pencil")
    ];

    /// <summary>Look up a sample product.</summary>
    /// <remarks>Use ID 1 for a success response or ID 999 for a business error.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ApiResponse<Product>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<Product>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ApiResponse<Product>> GetProduct(int id)
    {
        var product = Array.Find(Products, item => item.Id == id);
        if (product is null)
        {
            return NotFound(ApiResponseFactory.Failure<Product>(
                "PRODUCT_NOT_FOUND", "The product was not found."));
        }

        return Ok(ApiResponseFactory.Success(product));
    }

    /// <summary>List sample products with pagination metadata.</summary>
    /// <remarks>Use page 2 and pageSize 2 to see the last page.</remarks>
    [HttpGet("page")]
    [ProducesResponseType<ApiResponse<PaginatedResult<Product>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ApiResponse<PaginatedResult<Product>>> GetPage(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 2)
    {
        var totalPages = (Products.Length + pageSize - 1) / pageSize;
        return Ok(ApiResponseFactory.Success(new PaginatedResult<Product>
        {
            Items = page > totalPages ? [] : Products.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = Products.Length,
                TotalPages = totalPages
            }
        }));
    }

    /// <summary>Submit a product to see the validation response.</summary>
    /// <remarks>
    /// For HTTP 200, send <c>{"name":"Notebook","quantity":2}</c>.
    /// For HTTP 422 with two field errors, send <c>{"name":"","quantity":0}</c>.
    /// </remarks>
    [HttpPost("validate")]
    [ProducesResponseType<ApiResponse<ProductRequest>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<ApiResponse<ProductRequest>> ValidateProduct(ProductRequest product)
    {
        return Ok(ApiResponseFactory.Success(product));
    }

    /// <summary>Trigger a technical error and inspect the HTTP 500 envelope.</summary>
    [HttpGet("unexpected-error")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public IActionResult GetUnexpectedError()
    {
        throw new InvalidOperationException("Demonstration exception.");
    }
}

public sealed record Product(int Id, string Name);

public sealed record ProductRequest(
    [Required] string Name,
    [Range(1, 100)] int Quantity);
