
using Microsoft.Extensions.Options;
using Storix_BE.Service.Configuration;

namespace Storix_BE.API.Configuration
{
    public static class ThirdPartyServicesConfiguration
    {
        public static IServiceCollection AddThirdPartyServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.ThirdPartyServicesCollection();
            return services;
        }

        public static void ThirdPartyServicesCollection(this IServiceCollection services)
        {
            /*services.Configure<CloudinarySetting>(options =>
            {
                options.CloudinaryUrl = GetEnvironmentVariableOrThrow("CLOUDINARY_URL");
            });
            CloudinarySetting.Instance = services.BuildServiceProvider().GetService<IOptions<CloudinarySetting>>().Value;*/
        }

        private static string GetEnvironmentVariableOrThrow(string key)
        {
            return Environment.GetEnvironmentVariable(key)
                   ?? throw new ArgumentNullException(key, $"Environment variable '{key}' is not set.");
        }
    }
}
