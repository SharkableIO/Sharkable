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
            if (userId is null) return "Unauthorized".AsUnauthorized();
            return (object?)db.GetOrdersByUser(userId.Value).OrderByDescending(o => o.CreatedAt).ToList();
        }).RequireAuthorization();

        app.MapGet("{id:int}", (int id, HttpContext ctx, ShoppingDbContext db) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return "Unauthorized".AsUnauthorized();
            var order = db.GetOrderById(id);
            if (order is null || order.UserId != userId.Value)
                return "Not found".AsNotFound();
            return (object?)order;
        }).RequireAuthorization();

        app.MapPost("", async (CreateOrderRequest request, HttpContext ctx,
            ShoppingDbContext db, SagaExecutor sagaExecutor, OrderSagaSteps stepFactory,
            ILogger<OrderEndpoint> logger) =>
        {
            var userId = AuthEndpoint.GetUserId(ctx);
            if (userId is null) return "Unauthorized".AsUnauthorized();

            var cart = db.GetCart(userId.Value);
            if (cart is null || cart.Items.Count == 0)
                return "Cart is empty".AsBadRequest();

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
                return $"Order failed: {result.Error} (order #{orderId})".AsBadRequest();
            }

            db.UpdateOrder(orderId, order with { Status = OrderStatus.Shipped });
            db.RemoveCart(userId.Value);
            logger.LogInformation("Order {OrderId} placed successfully", orderId);
            return (object?)db.GetOrderById(orderId);
        }).RequireAuthorization();
    }
}
