using System;

namespace Storix_BE.Domain.Exception
{
    public sealed class BusinessRuleException : System.Exception
    {
        public string Code { get; }

        public BusinessRuleException(string code, string message)
            : base(message)
        {
            Code = code;
        }
    }
}
