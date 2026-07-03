using System.Collections.Concurrent;

namespace Sharkable.NativeTest;

public sealed class ShoppingDbContext
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private readonly ConcurrentDictionary<int, Cart> _carts = new();
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private readonly ConcurrentDictionary<int, Review> _reviews = new();
    private int _nextUserId = 1;
    private int _nextProductId = 1;
    private int _nextOrderId = 1;
    private int _nextReviewId = 1;

    private readonly object _userIdLock = new();
    private readonly object _productIdLock = new();
    private readonly object _orderIdLock = new();
    private readonly object _reviewIdLock = new();

    public int NextUserId() { lock (_userIdLock) { var id = _nextUserId; _nextUserId++; return id; } }
    public int NextProductId() { lock (_productIdLock) { var id = _nextProductId; _nextProductId++; return id; } }
    public int NextOrderId() { lock (_orderIdLock) { var id = _nextOrderId; _nextOrderId++; return id; } }
    public int NextReviewId() { lock (_reviewIdLock) { var id = _nextReviewId; _nextReviewId++; return id; } }

    public User? GetUserById(int id) => _users.TryGetValue(id, out var u) ? u : null;
    public User? GetUserByUsername(string username) => _users.Values.FirstOrDefault(u => u.Username == username);
    public User? GetUserByEmail(string email) => _users.Values.FirstOrDefault(u => u.Email == email);
    public IEnumerable<User> GetAllUsers() => _users.Values;
    public void AddUser(User user) => _users[user.Id] = user;
    public bool DeleteUser(int id) => _users.TryRemove(id, out _);
    public int UserCount => _users.Count;

    public Product? GetProductById(int id) => _products.TryGetValue(id, out var p) ? p : null;
    public IEnumerable<Product> GetAllProducts() => _products.Values;
    public IEnumerable<Product> GetProductsByCategory(string category) =>
        _products.Values.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<Product> SearchProducts(string query) =>
        _products.Values.Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
    public void AddProduct(Product product) => _products[product.Id] = product;
    public bool UpdateProduct(int id, Product product)
    {
        if (!_products.ContainsKey(id)) return false;
        _products[id] = product;
        return true;
    }
    public bool DeleteProduct(int id) => _products.TryRemove(id, out _);
    public int ProductCount => _products.Count;

    public Cart? GetCart(int userId) => _carts.TryGetValue(userId, out var c) ? c : null;
    public void SetCart(int userId, Cart cart) => _carts[userId] = cart;
    public bool RemoveCart(int userId) => _carts.TryRemove(userId, out _);

    public Order? GetOrderById(int id) => _orders.TryGetValue(id, out var o) ? o : null;
    public IEnumerable<Order> GetOrdersByUser(int userId) =>
        _orders.Values.Where(o => o.UserId == userId);
    public IEnumerable<Order> GetAllOrders() => _orders.Values;
    public void AddOrder(Order order) => _orders[order.Id] = order;
    public bool UpdateOrder(int id, Order order)
    {
        if (!_orders.ContainsKey(id)) return false;
        _orders[id] = order;
        return true;
    }
    public int OrderCount => _orders.Count;

    public Review? GetReviewById(int id) => _reviews.TryGetValue(id, out var r) ? r : null;
    public IEnumerable<Review> GetReviewsByProduct(int productId) =>
        _reviews.Values.Where(r => r.ProductId == productId);
    public void AddReview(Review review) => _reviews[review.Id] = review;
}
