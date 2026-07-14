using System.Net;

namespace Sharkable.NativeTest;

[SharkDescription("Admin Dashboard", "User management, order management, and system stats")]
[SharkTag("admin")]
[EndpointGroup("admin")]
[SharkDeprecated]
public class AdminEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("users", (HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return "Forbidden".AsForbidden();
            return (object?)db.GetAllUsers().Select(u => new AdminUserInfo(
                u.Id, u.Username, u.Email, u.DisplayName,
                u.Role, u.CreatedAt,
                db.GetOrdersByUser(u.Id).Count()
            )).ToList();
        }).RequireAuthorization();

        app.MapDelete("users/{id:int}", (int id, HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return "Forbidden".AsForbidden();
            if (db.GetUserById(id) is null) return "Not found".AsNotFound();
            if (id == AuthEndpoint.GetUserId(ctx))
                return "Cannot delete yourself".AsBadRequest();
            db.DeleteUser(id);
            return (object?)new UnifiedResult<object?>(null, null, HttpStatusCode.OK);
        }).RequireAuthorization();

        app.MapGet("orders", (HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return "Forbidden".AsForbidden();
            return (object?)db.GetAllOrders().OrderByDescending(o => o.CreatedAt).ToList();
        }).RequireAuthorization();

        app.MapPut("orders/{id:int}/status", ([Microsoft.AspNetCore.Mvc.FromBody] OrderStatus newStatus, int id, HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return "Forbidden".AsForbidden();
            var order = db.GetOrderById(id);
            if (order is null) return "Not found".AsNotFound();
            var updated = order with
            {
                Status = newStatus,
                CompletedAt = newStatus is OrderStatus.Delivered or OrderStatus.Cancelled ? DateTime.UtcNow : order.CompletedAt
            };
            db.UpdateOrder(id, updated);
            return (object?)updated;
        }).RequireAuthorization();

        app.MapGet("stats", (HttpContext ctx, ShoppingDbContext db) =>
        {
            if (!ctx.User.IsInRole("Admin")) return "Forbidden".AsForbidden();
            var allOrders = db.GetAllOrders().ToList();
            return (object?)new AdminStats(
                db.UserCount,
                db.ProductCount,
                db.OrderCount,
                allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
                Enum.GetValues<OrderStatus>().ToDictionary(
                    s => s.ToString(), s => allOrders.Count(o => o.Status == s)));
        }).RequireAuthorization();
    }
}
