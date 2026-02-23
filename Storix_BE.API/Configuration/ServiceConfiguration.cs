
using Storix_BE.API.BackgroundJobs;
using Storix_BE.API.Filters;
using Storix_BE.Service.Implementation;
using Storix_BE.Service.Interfaces;
using Storix_BE.Service.Configuration;

namespace Storix_BE.API.Configuration
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServiceConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MomoGatewayOptions>(configuration.GetSection("PaymentGateways:MoMo"));
            services.Configure<PaymentRuntimeOptions>(configuration.GetSection("PaymentSettings"));
            services.AddHttpClient<IMomoAtmGatewayService, MomoAtmGatewayService>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWarehouseAssignmentService, WarehouseAssignmentService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IInventoryInboundService, InventoryInboundService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IInventoryOutboundService, InventoryOutboundService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IStockCountService, StockCountService>();
            services.AddTransient<IImageService, ImageService>();
            services.AddTransient<IEmailService, EmailService>();

            // Filter kiểm tra subscription (Scoped vì phụ thuộc ISubscriptionService)
            services.AddScoped<SubscriptionAccessFilter>();

            // Background job tự động expire subscriptions mỗi giờ
            services.AddHostedService<SubscriptionExpiryJob>();

            return services;
        }
    }
}
