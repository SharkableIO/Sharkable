using Sharkable;

namespace Sharkable.NativeTest;

public sealed class OrderSagaSteps
{
    private readonly ShoppingDbContext _db;
    private readonly ILogger<OrderSagaSteps> _logger;

    public OrderSagaSteps(ShoppingDbContext db, ILogger<OrderSagaSteps> logger)
    {
        _db = db;
        _logger = logger;
    }

    public ISagaStep CreateReserveInventoryStep(int productId, int quantity)
        => new ReserveInventoryStep(_db, _logger, productId, quantity);

    public ISagaStep CreateProcessPaymentStep(int orderId, decimal amount)
        => new ProcessPaymentStep(_logger, orderId, amount);

    public ISagaStep CreateShipOrderStep(int orderId)
        => new ShipOrderStep(_db, _logger, orderId);

    private sealed class ReserveInventoryStep(
        ShoppingDbContext db,
        ILogger<OrderSagaSteps> logger,
        int productId,
        int quantity) : ISagaStep
    {
        public Task<SagaResult> ExecuteAsync(CancellationToken ct)
        {
            var product = db.GetProductById(productId);
            if (product is null)
                return Task.FromResult(new SagaResult(false, $"Product {productId} not found"));
            if (product.Stock < quantity)
                return Task.FromResult(new SagaResult(false, $"Insufficient stock for {product.Name}"));

            var updated = product with { Stock = product.Stock - quantity };
            db.UpdateProduct(productId, updated);
            logger.LogInformation("Reserved {Qty}x {Product} (stock left: {Stock})", quantity, product.Name, updated.Stock);
            return Task.FromResult(new SagaResult(true));
        }

        public Task CompensateAsync(CancellationToken ct)
        {
            var product = db.GetProductById(productId);
            if (product is not null)
            {
                var restored = product with { Stock = product.Stock + quantity };
                db.UpdateProduct(productId, restored);
                logger.LogInformation("Released {Qty}x {Product} (stock restored: {Stock})", quantity, product.Name, restored.Stock);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class ProcessPaymentStep(
        ILogger<OrderSagaSteps> logger,
        int orderId,
        decimal amount) : ISagaStep
    {
        public Task<SagaResult> ExecuteAsync(CancellationToken ct)
        {
            if (amount <= 0)
                return Task.FromResult(new SagaResult(false, "Invalid payment amount"));

            logger.LogInformation("Payment processed for order {OrderId}: ${Amount:F2}", orderId, amount);
            return Task.FromResult(new SagaResult(true));
        }

        public Task CompensateAsync(CancellationToken ct)
        {
            logger.LogInformation("Payment refunded for order {OrderId}: ${Amount:F2}", orderId, amount);
            return Task.CompletedTask;
        }
    }

    private sealed class ShipOrderStep(
        ShoppingDbContext db,
        ILogger<OrderSagaSteps> logger,
        int orderId) : ISagaStep
    {
        public Task<SagaResult> ExecuteAsync(CancellationToken ct)
        {
            var order = db.GetOrderById(orderId);
            if (order is null)
                return Task.FromResult(new SagaResult(false, $"Order {orderId} not found"));

            var updated = order with { Status = OrderStatus.Shipped };
            db.UpdateOrder(orderId, updated);
            logger.LogInformation("Order {OrderId} shipped", orderId);
            return Task.FromResult(new SagaResult(true));
        }

        public Task CompensateAsync(CancellationToken ct)
        {
            logger.LogInformation("Shipment cancelled for order {OrderId}", orderId);
            return Task.CompletedTask;
        }
    }
}

public sealed class PlaceOrderSaga : Saga
{
    public PlaceOrderSaga(IEnumerable<ISagaStep> steps)
    {
        foreach (var step in steps)
            AddStep(step);
    }
}
