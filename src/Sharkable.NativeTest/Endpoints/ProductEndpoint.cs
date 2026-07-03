namespace Sharkable.NativeTest;

[SharkDescription("Products", "Browse, search, and manage products")]
[SharkTag("products")]
public class ProductEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", (ShoppingDbContext db, HttpContext ctx) =>
        {
            var category = ctx.Request.Query["category"].FirstOrDefault();
            var search = ctx.Request.Query["search"].FirstOrDefault();

            IEnumerable<Product> products;
            if (!string.IsNullOrEmpty(category))
                products = db.GetProductsByCategory(category);
            else if (!string.IsNullOrEmpty(search))
                products = db.SearchProducts(search);
            else
                products = db.GetAllProducts();

            return Results.Ok(products.OrderBy(p => p.Id).ToList());
        });

        app.MapGet("{id:int}", (int id, ShoppingDbContext db) =>
        {
            var product = db.GetProductById(id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });

        app.MapPost("", (CreateProductRequest request, ShoppingDbContext db, HttpContext ctx) =>
        {
            if (!ctx.User.IsInRole("Admin"))
                return Results.Forbid();
            var product = new Product(
                db.NextProductId(), request.Name, request.Description,
                request.Price, request.Stock, request.Category,
                request.ImageUrl, DateTime.UtcNow);
            db.AddProduct(product);
            return Results.Created($"/api/products/{product.Id}", product);
        }).RequireAuthorization();

        app.MapPut("{id:int}", (int id, UpdateProductRequest request, ShoppingDbContext db, HttpContext ctx) =>
        {
            if (!ctx.User.IsInRole("Admin"))
                return Results.Forbid();
            var existing = db.GetProductById(id);
            if (existing is null) return Results.NotFound();

            var updated = existing with
            {
                Name = request.Name ?? existing.Name,
                Description = request.Description ?? existing.Description,
                Price = request.Price ?? existing.Price,
                Stock = request.Stock ?? existing.Stock,
                Category = request.Category ?? existing.Category,
                ImageUrl = request.ImageUrl ?? existing.ImageUrl,
            };
            db.UpdateProduct(id, updated);
            return Results.Ok(updated);
        }).RequireAuthorization();

        app.MapDelete("{id:int}", (int id, ShoppingDbContext db, HttpContext ctx) =>
        {
            if (!ctx.User.IsInRole("Admin"))
                return Results.Forbid();
            return db.DeleteProduct(id) ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization();
    }
}
