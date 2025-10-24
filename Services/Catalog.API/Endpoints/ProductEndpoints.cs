using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;

namespace Catalog.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products").WithTags("Product");

        group.MapGet("/", async (CatalogDbContext dbContext) =>
        {
            var products = await dbContext.Products.ToListAsync();
            return Results.Ok(products);
        })
        .WithName("GetAllProducts");

        // GET: /api/products/GUID
        group.MapGet("/{id}", async (Guid id, CatalogDbContext dbContext) =>
        {
            var product = await dbContext.Products.FindAsync(id);

            return product is not null
                ? Results.Ok(product)
                : Results.NotFound();
        })
        .WithName("GetProductById");

        // POST: /api/products
        group.MapPost("/", async (ProductRequest request, CatalogDbContext dbContext) =>
        {
            var newProduct = new Product
            {
                Id = Guid.NewGuid(), // Hasilkan ID baru di server
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            dbContext.Products.Add(newProduct);
            await dbContext.SaveChangesAsync();

            // Kembalikan HTTP 201 (Created) dengan lokasi produk baru
            return Results.CreatedAtRoute("GetProductById", new { id = newProduct.Id }, newProduct);
        })
        .WithName("CreateProduct");

        // PUT: /api/products/GUID
        group.MapPut("/{id}", async (Guid id, ProductRequest request, CatalogDbContext dbContext) =>
        {
            var existingProduct = await dbContext.Products.FindAsync(id);

            if (existingProduct is null)
            {
                return Results.NotFound();
            }

            // Update properti produk yang ada
            existingProduct.Name = request.Name;
            existingProduct.Description = request.Description;
            existingProduct.Price = request.Price;
            existingProduct.StockQuantity = request.StockQuantity;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("UpdateProduct");

        // DELETE: /api/products/GUID
        group.MapDelete("/{id}", async (Guid id, CatalogDbContext dbContext) =>
        {
            var product = await dbContext.Products.FindAsync(id);

            if (product is null)
            {
                return Results.NotFound();
            }

            dbContext.Products.Remove(product);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteProduct");
    }
}