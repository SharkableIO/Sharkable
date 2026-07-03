using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Sharkable.NativeTest;

public sealed class AuthService
{
    private readonly ShoppingDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(ShoppingDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public AuthResponse? Login(LoginRequest request)
    {
        var user = _db.GetUserByUsername(request.Username);
        if (user is null) return null;
        if (!VerifyPassword(request.Password, user.PasswordHash)) return null;
        var token = GenerateToken(user);
        return new AuthResponse(token, ToInfo(user));
    }

    public (AuthResponse? Response, string? Error) Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return (null, "Username must be at least 3 characters");
        if (_db.GetUserByUsername(request.Username) is not null)
            return (null, "Username already taken");
        if (_db.GetUserByEmail(request.Email) is not null)
            return (null, "Email already registered");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return (null, "Password must be at least 6 characters");

        var role = _db.UserCount == 0 ? UserRole.Admin : UserRole.Customer;
        var user = new User(
            _db.NextUserId(), request.Username, request.Email,
            HashPassword(request.Password), request.DisplayName,
            role, DateTime.UtcNow);
        _db.AddUser(user);
        var token = GenerateToken(user);
        return (new AuthResponse(token, ToInfo(user)), null);
    }

    public UserInfo? GetProfile(int userId)
    {
        var user = _db.GetUserById(userId);
        return user is null ? null : ToInfo(user);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "DefaultSampleKey12345678901234567890"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "sharkable-shop",
            audience: _config["Jwt:Audience"] ?? "sharkable-shop-api",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        var result = new byte[48];
        Buffer.BlockCopy(salt, 0, result, 0, 16);
        Buffer.BlockCopy(hash, 0, result, 16, 32);
        return Convert.ToBase64String(result);
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var bytes = Convert.FromBase64String(stored);
        if (bytes.Length != 48) return false;
        var salt = bytes[..16];
        var storedHash = bytes[16..];
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    private static UserInfo ToInfo(User user) => new(
        user.Id, user.Username, user.Email, user.DisplayName, user.Role, user.CreatedAt);
}
