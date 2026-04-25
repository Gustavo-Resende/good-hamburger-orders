using Ardalis.Result.AspNetCore;
using GoodHamburger.API.ExceptionHandling;

namespace GoodHamburger.API.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers(options => options.AddDefaultResultConvention());
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCors(options =>
        {
            options.AddPolicy("BlazorPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:5192", "https://localhost:7254")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
