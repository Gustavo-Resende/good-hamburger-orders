using Microsoft.Extensions.DependencyInjection;

namespace GoodHamburger.Application.DependencyInjection;

public static class MediatRConfig
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MediatRConfig).Assembly));
        return services;
    }
}
