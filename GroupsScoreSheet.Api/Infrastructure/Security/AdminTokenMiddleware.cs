using System.Security.Cryptography;
using System.Text;

namespace GroupsScoreSheet.Api.Infrastructure.Security;

public sealed class AdminTokenMiddleware
{
    private const string AdminPathPrefix = "/api/admin";
    private const string AdminTokenHeaderName = "X-Admin-Token";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminTokenMiddleware> _logger;

    public AdminTokenMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AdminTokenMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(AdminPathPrefix))
        {
            await _next(context);
            return;
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var configuredToken = _configuration["AdminAccess:Token"];

        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            _logger.LogError("Admin token is not configured.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Admin token is not configured."
            });

            return;
        }

        if (!context.Request.Headers.TryGetValue(AdminTokenHeaderName, out var providedTokenValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Admin token is required.",
                requiredHeader = AdminTokenHeaderName
            });

            return;
        }

        var providedToken = providedTokenValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedToken) || !TokenEquals(configuredToken, providedToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Invalid admin token."
            });

            return;
        }

        await _next(context);
    }

    private static bool TokenEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}