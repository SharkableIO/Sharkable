namespace Sharkable.NativeTest;

[SharkDescription("Admin Dashboard", "User management, order management, and system stats")]
[SharkTag("admin")]
[EndpointGroup("admin")]
public class AdminEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("users", (HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return Results.Forbid();
            return Results.Ok(db.GetAllUsers().Select(u => new AdminUserInfo(
                u.Id, u.Username, u.Email, u.DisplayName,
                u.Role, u.CreatedAt,
                db.GetOrdersByUser(u.Id).Count()
            )).ToList());
        }).RequireAuthorization();

        app.MapDelete("users/{id:int}", (int id, HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return Results.Forbid();
            if (db.GetUserById(id) is null) return Results.NotFound();
            if (id == AuthEndpoint.GetUserId(ctx))
                return Results.BadRequest(new ErrorResponse("Cannot delete yourself"));
            db.DeleteUser(id);
            return Results.Ok();
        }).RequireAuthorization();

        app.MapGet("orders", (HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return Results.Forbid();
            return Results.Ok(db.GetAllOrders().OrderByDescending(o => o.CreatedAt).ToList());
        }).RequireAuthorization();

        app.MapPut("orders/{id:int}/status", ([Microsoft.AspNetCore.Mvc.FromBody] OrderStatus newStatus, int id, HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return Results.Forbid();
            var order = db.GetOrderById(id);
            if (order is null) return Results.NotFound();
            var updated = order with
            {
                Status = newStatus,
                CompletedAt = newStatus is OrderStatus.Delivered or OrderStatus.Cancelled ? DateTime.UtcNow : order.CompletedAt
            };
            db.UpdateOrder(id, updated);
            return Results.Ok(updated);
        }).RequireAuthorization();

        app.MapGet("stats", (HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return Results.Forbid();
            var allOrders = db.GetAllOrders().ToList();
            return Results.Ok(new AdminStats(
                db.UserCount,
                db.ProductCount,
                db.OrderCount,
                allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
                Enum.GetValues<OrderStatus>().ToDictionary(
                    s => s.ToString(), s => allOrders.Count(o => o.Status == s))));
        }).RequireAuthorization();
    }
}
