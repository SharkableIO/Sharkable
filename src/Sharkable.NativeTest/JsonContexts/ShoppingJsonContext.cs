using System.Text.Json.Serialization;
using Sharkable;
using Sharkable.AutoCrud.SqlSugar;

namespace Sharkable.NativeTest;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(AdminUserInfo))]
[JsonSerializable(typeof(AdminUserInfo[]))]
[JsonSerializable(typeof(List<AdminUserInfo>))]
[JsonSerializable(typeof(AdminStats))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(CreateProductRequest))]
[JsonSerializable(typeof(UpdateProductRequest))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(Product[]))]
[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(AddToCartRequest))]
[JsonSerializable(typeof(UpdateCartItemRequest))]
[JsonSerializable(typeof(Cart))]
[JsonSerializable(typeof(CartItem))]
[JsonSerializable(typeof(CartItem[]))]
[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Order[]))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(OrderStatus))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(Dictionary<string, HealthCheckEntry>))]
// Health check types (from Sharkable library)
[JsonSerializable(typeof(HealthCheckEntry[]))]
[JsonSerializable(typeof(List<HealthCheckEntry>))]
[JsonSerializable(typeof(HealthCheckResponse))]
// Auto-wrap response types
[JsonSerializable(typeof(UnifiedResult<Product>))]
[JsonSerializable(typeof(UnifiedResult<Product[]>))]
[JsonSerializable(typeof(UnifiedResult<List<Product>>))]
[JsonSerializable(typeof(UnifiedResult<Cart>))]
[JsonSerializable(typeof(UnifiedResult<Order>))]
[JsonSerializable(typeof(UnifiedResult<Order[]>))]
[JsonSerializable(typeof(UnifiedResult<List<Order>>))]
[JsonSerializable(typeof(UnifiedResult<UserInfo>))]
[JsonSerializable(typeof(UnifiedResult<AdminUserInfo[]>))]
[JsonSerializable(typeof(UnifiedResult<List<AdminUserInfo>>))]
[JsonSerializable(typeof(UnifiedResult<object?>))]
[JsonSerializable(typeof(UnifiedResult<AdminStats>))]
[JsonSerializable(typeof(UnifiedResult<ErrorResponse>))]
[JsonSerializable(typeof(UnifiedResult<string>))]
// AutoCrud test types
[JsonSerializable(typeof(TestItem))]
[JsonSerializable(typeof(TestItem[]))]
[JsonSerializable(typeof(List<TestItem>))]
[JsonSerializable(typeof(UnifiedResult<TestItem>))]
[JsonSerializable(typeof(UnifiedResult<TestItem[]>))]
[JsonSerializable(typeof(UnifiedResult<List<TestItem>>))]
[JsonSerializable(typeof(UnifiedResult<object?>))]
// AutoCrud PagedResult
[JsonSerializable(typeof(PagedResult<object>))]
[JsonSerializable(typeof(UnifiedResult<PagedResult<object>>))]
internal partial class ShoppingJsonContext : JsonSerializerContext
{
}
