using PetCloud.DTOs.Common;

namespace PetCloud.Infrastructure {
    public class ApiExceptionMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) {
            if (!context.Request.Path.StartsWithSegments("/api")) {
                await _next(context);
                return;
            }

            try {
                await _next(context);
            } catch (Exception ex) {
                _logger.LogError(ex, "Unhandled API exception at {Path}", context.Request.Path);

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new ApiErrorResponse {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }
    }
}
