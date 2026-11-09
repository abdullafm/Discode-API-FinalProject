using Discode_API_FinalProject.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Discode.Tests.Integration
{
    public class ApiFactory : WebApplicationFactory<Program>
    {
        public TestEmail Mail { get; } = new();
        public FakeStripeGateway Stripe { get; } = new();

        private readonly string _database = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                foreach (var hosted in services.Where(descriptor =>
                             descriptor.ImplementationType == typeof(VoiceGhostSweeper) || descriptor.ImplementationType == typeof(NitroExpiryWorker) || descriptor.ImplementationType == typeof(StatusClearWorker)).ToList())
                {
                    services.Remove(hosted);
                }

                foreach (var descriptor in services.Where(item =>
                             item.ServiceType == typeof(AppDbContext) ||
                             item.ServiceType == typeof(DbContextOptions) ||
                             item.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList())
                {
                    services.Remove(descriptor);
                }

                foreach (var descriptor in services.Where(item => item.ServiceType == typeof(IEmailRepository)).ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IEmailRepository>(Mail);

                foreach (var descriptor in services.Where(item => item.ServiceType == typeof(Service.Services.Interfaces.IStripeGateway)).ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(Stripe);
                services.AddSingleton<Service.Services.Interfaces.IStripeGateway>(Stripe);
                services.AddDbContext<AppDbContext>((provider, options) =>
                    options.UseInMemoryDatabase(_database)
                           .AddInterceptors(provider.GetRequiredService<Repository.Interceptors.AdminChangeInterceptor>()));
            });
        }
    }
}
