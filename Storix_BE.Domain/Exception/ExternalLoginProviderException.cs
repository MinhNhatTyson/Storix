using System;

namespace Storix_BE.Domain.Exception
{
    public sealed class ExternalLoginProviderException : System.Exception
    {
        public string Provider { get; }

        public ExternalLoginProviderException(string provider, string message)
            : base(message)
        {
            Provider = provider;
        }
    }
}
