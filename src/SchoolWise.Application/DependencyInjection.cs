using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SchoolWise.Application.Common;

namespace SchoolWise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, string? autoMapperLicenseKey = null)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(options =>
        {
            if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
            {
                options.LicenseKey = autoMapperLicenseKey;
            }
        }, assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
