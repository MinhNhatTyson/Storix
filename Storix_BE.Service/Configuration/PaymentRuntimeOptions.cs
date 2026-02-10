namespace Storix_BE.Service.Configuration
{
    public sealed class PaymentRuntimeOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
    }
}
