using Comprehension.Data;
using Microsoft.EntityFrameworkCore;

namespace Comprehension.Helpers
{
    public class AuthBearer
    {
        private readonly RequestDelegate _next;

        public AuthBearer(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ComprehensionContext db)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (path.Contains("/api/auth/"))
            {
                await _next(context);
                return;
            }

            var header = context.Request.Headers["Authorization"].FirstOrDefault();
            if (header == null || !header.StartsWith("Bearer "))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized - missing Bearer token" });
                return;
            }

            var token = header.Substring("Bearer ".Length);

            var user = await db.Users.FirstOrDefaultAsync(
                u => u.Token == token && u.TokenExpiration > DateTime.UtcNow
            );

            if (user == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized - invalid or expired token" });
                return;
            }

            context.Items["User"] = user;
            await _next(context);
        }
    }

    public static class AuthBearerExtensions
    {
        public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthBearer>();
        }
    }
}
