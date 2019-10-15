using System;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using PluginC.Calculations;
using Prise.Infrastructure;

namespace PluginC
{
    [PluginBootstrapper(PluginType = typeof(DivideOrMultiplyCalculationPlugin))]
    public class DivideOrMultiplyCalculationBootstrapper : IPluginBootstrapper
    {
        public IServiceCollection Bootstrap(IServiceCollection services)
        {
            // Add a fixed discount of 10%
            services.AddSingleton<IDiscount>(new Discount(1.10m));
            services.AddScoped<IDiscountService, DiscountService>();

            // Randomly choose what 3rd party service to use
            var random = new Random();
            if (random.Next() % 2 == 0)
                services.AddScoped<ICanCalculate, DivideCalculation>();
            else
                services.AddScoped<ICanCalculate, MultiplyCalculation>();

            return services;
        }
    }
}