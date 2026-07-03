namespace Sharkable.NativeTest;

public enum UserRole { Customer, Admin }
public enum OrderStatus { Pending, Paid, Shipped, Delivered, Cancelled }

public sealed record User(
    int Id,
    string Username,
    string Email,
    string PasswordHash,
    string DisplayName,
    UserRole Role,
    DateTime CreatedAt);

public sealed record LoginRequest(string Username, string Password);
public sealed record RegisterRequest(string Username, string Email, string Password, string DisplayName);

public sealed record AuthResponse(string Token, UserInfo User);

public sealed record UserInfo(
    int Id,
    string Username,
    string Email,
    string DisplayName,
    UserRole Role,
    DateTime CreatedAt);

public sealed record Product(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string ImageUrl,
    DateTime CreatedAt);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string ImageUrl);

public sealed record UpdateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    int? Stock,
    string? Category,
    string? ImageUrl);

public sealed record CartItem(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record Cart(int UserId, List<CartItem> Items, decimal Total);

public sealed record AddToCartRequest(int ProductId, int Quantity);
public sealed record UpdateCartItemRequest(int ProductId, int Quantity);

public sealed record Order(
    int Id,
    int UserId,
    List<OrderItem> Items,
    decimal Total,
    OrderStatus Status,
    string ShippingAddress,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record OrderItem(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record CreateOrderRequest(string ShippingAddress);

public sealed record Review(
    int Id,
    int UserId,
    int ProductId,
    int Rating,
    string Comment,
    DateTime CreatedAt);

public sealed record CreateReviewRequest(int ProductId, int Rating, string Comment);
