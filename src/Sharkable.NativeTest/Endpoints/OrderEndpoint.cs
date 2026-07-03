using Sharkable;

namespace Sharkable.NativeTest;

[SharkDescription("Orders", "Place, manage, and track orders via SAGA")]
[SharkTag("orders")]
public class OrderEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", (HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var orders = db.GetOrdersByUser(userId.Value).OrderByDescending(o => o.CreatedAt).ToList();
            return Results.Ok(orders);
        }).RequireAuthorization();

        app.MapGet("{id:int}", (int id, HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var order = db.GetOrderById(id);
            if (order is null || order.UserId != userId.Value)
                return Results.NotFound();
            return Results.Ok(order);
        }).RequireAuthorization();

        app.MapPost("", async (CreateOrderRequest request, HttpContext ctx,
            ShoppingDbContext db, SagaExecutor sagaExecutor, OrderSagaSteps stepFactory,
            ILogger<OrderEndpoint> logger) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var cart = db.GetCart(userId.Value);
            if (cart is null || cart.Items.Count == 0)
                return Results.BadRequest(new ErrorResponse("Cart is empty"));

            var orderId = db.NextOrderId();
            var orderItems = cart.Items.Select(i =>
                new OrderItem(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList();
            var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            var order = new Order(orderId, userId.Value, orderItems, total,
                OrderStatus.Pending, request.ShippingAddress, DateTime.UtcNow, null);
            db.AddOrder(order);

            var sagaSteps = new List<ISagaStep>();
            foreach (var item in cart.Items)
                sagaSteps.Add(stepFactory.CreateReserveInventoryStep(item.ProductId, item.Quantity));
            sagaSteps.Add(stepFactory.CreateProcessPaymentStep(orderId, total));
            sagaSteps.Add(stepFactory.CreateShipOrderStep(orderId));

            var saga = new PlaceOrderSaga(sagaSteps);
            var sagaId = $"order:{orderId}:{Guid.NewGuid():N}";
            var result = await sagaExecutor.ExecuteAsync(sagaId, saga);

            if (!result.Success)
            {
                db.UpdateOrder(orderId, order with { Status = OrderStatus.Cancelled });
                logger.LogWarning("Order {OrderId} failed at step {Step}: {Error}",
                    orderId, result.FailedStepIndex, result.Error);
                return Results.BadRequest(new ErrorResponse($"{result.Error} (order #{orderId})"));
            }

            db.UpdateOrder(orderId, order with { Status = OrderStatus.Shipped });
            db.RemoveCart(userId.Value);
            logger.LogInformation("Order {OrderId} placed successfully", orderId);
            return Results.Ok(db.GetOrderById(orderId));
        }).RequireAuthorization();
    }
}
