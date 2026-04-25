using Microsoft.AspNetCore.Diagnostics;

namespace GoodHamburger.API.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "Ocorreu um erro interno." }, cancellationToken);
        return true;
    }
}
