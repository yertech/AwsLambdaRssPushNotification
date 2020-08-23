using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace AWSLambdaRssNotification.Helper
{
    public class DependencyResolver
    {
        public IServiceProvider ServiceProvider { get; }
        public Action<IServiceCollection> RegisterServices { get; }

        public DependencyResolver(Action<IServiceCollection> registerServices = null)
        {
            // Set up Dependency Injection
            var serviceCollection = new ServiceCollection();
            RegisterServices = registerServices;
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IConfigurationService, ConfigurationService>();
            services.AddAutoMapper(System.Reflection.Assembly.GetExecutingAssembly());
            // Register other services
            RegisterServices?.Invoke(services);
        }
    }
}
