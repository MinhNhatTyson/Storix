using System;

namespace Storix_BE.Domain.Exception
{
    public class BusinessRuleException : InvalidOperationException
    {
        public string Code { get; }

        public BusinessRuleException(string code, string message)
            : base($"{code}: {message}")
        {
            Code = code;
        }
    }
}
