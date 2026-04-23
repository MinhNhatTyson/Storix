using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.Service.Interfaces
{
    public interface ISkuService
    {
        /// <summary>
        /// Generates a structured SKU for a new product and advances the company sequence.
        /// </summary>
        Task<string> GenerateSkuAsync(
            int companyId,
            string? supplierName,
            string categoryCode,
            string? packageType,
            string? sizeStandard,
            bool isEsd,
            bool isMsd,
            bool isCold,
            bool isVulnerable,
            bool isHighValue,
            CancellationToken ct = default);
    }
}
