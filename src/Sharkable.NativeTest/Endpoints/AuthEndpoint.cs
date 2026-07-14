using System.Security.Claims;

namespace Sharkable.NativeTest;

[SharkDescription("Authentication", "Register, login, and profile management")]
[SharkTag("auth")]
public class AuthEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("register", (RegisterRequest request, AuthService auth) =>
        {
            var (response, error) = auth.Register(request);
            return error is not null
                ? (object?)error.AsBadRequest()
                : response;
        }).SharkRequireRateLimiting("auth");

        app.MapPost("login", (LoginRequest request, AuthService auth) =>
        {
            var response = auth.Login(request);
            return response is not null
                ? (object?)response
                : "Unauthorized".AsUnauthorized();
        }).SharkRequireRateLimiting("auth");

        app.MapGet("profile", (HttpContext ctx, AuthService auth) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return "Not found".AsNotFound();
            var profile = auth.GetProfile(userId.Value);
            return profile is not null ? (object?)profile : "Not found".AsNotFound();
        }).RequireAuthorization();
    }

    public static int? GetUserId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
