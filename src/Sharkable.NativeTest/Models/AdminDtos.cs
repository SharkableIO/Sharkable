namespace Sharkable.NativeTest;

public sealed record AdminUserInfo(
    int Id,
    string Username,
    string Email,
    string DisplayName,
    UserRole Role,
    DateTime CreatedAt,
    int OrderCount);

public sealed record AdminStats(
    int TotalUsers,
    int TotalProducts,
    int TotalOrders,
    decimal Revenue,
    Dictionary<string, int> OrdersByStatus);
