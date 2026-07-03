namespace Sharkable.NativeTest;

[SharkDescription("Shopping Cart", "Manage your shopping cart")]
[SharkTag("cart")]
public class CartEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", (HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var cart = db.GetCart(userId.Value);
            return Results.Ok(cart ?? new Cart(userId.Value, [], 0));
        }).RequireAuthorization();

        app.MapPost("items", (AddToCartRequest request, HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var product = db.GetProductById(request.ProductId);
            if (product is null) return Results.BadRequest(new ErrorResponse("Product not found"));
            if (request.Quantity <= 0) return Results.BadRequest(new ErrorResponse("Quantity must be positive"));
            if (product.Stock < request.Quantity) return Results.BadRequest(new ErrorResponse("Insufficient stock"));

            var cart = db.GetCart(userId.Value) ?? new Cart(userId.Value, [], 0);
            var items = new List<CartItem>(cart.Items);
            var existing = items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existing is not null)
            {
                items.Remove(existing);
                items.Add(existing with { Quantity = existing.Quantity + request.Quantity });
            }
            else
            {
                items.Add(new CartItem(product.Id, product.Name, product.Price, request.Quantity));
            }
            var total = items.Sum(i => i.UnitPrice * i.Quantity);
            db.SetCart(userId.Value, new Cart(userId.Value, items, total));
            return Results.Ok(new Cart(userId.Value, items, total));
        }).RequireAuthorization();

        app.MapPut("items", (UpdateCartItemRequest request, HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var cart = db.GetCart(userId.Value);
            if (cart is null) return Results.NotFound();

            var items = new List<CartItem>(cart.Items);
            var existing = items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existing is null) return Results.NotFound();

            items.Remove(existing);
            if (request.Quantity > 0)
            {
                var product = db.GetProductById(request.ProductId);
                if (product is null) return Results.BadRequest(new ErrorResponse("Product not found"));
                items.Add(existing with { Quantity = request.Quantity });
            }
            var total = items.Sum(i => i.UnitPrice * i.Quantity);
            db.SetCart(userId.Value, new Cart(userId.Value, items, total));
            return Results.Ok(new Cart(userId.Value, items, total));
        }).RequireAuthorization();

        app.MapDelete("items/{productId:int}", (int productId, HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var cart = db.GetCart(userId.Value);
            if (cart is null) return Results.NotFound();

            var items = cart.Items.Where(i => i.ProductId != productId).ToList();
            var total = items.Sum(i => i.UnitPrice * i.Quantity);
            db.SetCart(userId.Value, new Cart(userId.Value, items, total));
            return Results.Ok(new Cart(userId.Value, items, total));
        }).RequireAuthorization();

        app.MapDelete("", (HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            db.RemoveCart(userId.Value);
            return Results.Ok(new Cart(userId.Value, [], 0));
        }).RequireAuthorization();
    }
}
