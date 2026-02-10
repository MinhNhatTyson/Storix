using Storix_BE.Domain.Models;
using Storix_BE.Domain.Exception;
using Storix_BE.Repository.Interfaces;
using Storix_BE.Service.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Storix_BE.Service.Implementation
{
    public class PaymentService : IPaymentService
    {
        private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "MANUAL",
            "MOMO",
            "VNPAY"
        };

        private readonly IPaymentRepository _paymentRepository;
        private readonly IMomoAtmGatewayService _momoAtmGatewayService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IMomoAtmGatewayService momoAtmGatewayService,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _momoAtmGatewayService = momoAtmGatewayService;
            _logger = logger;
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, int callerCompanyId)
        {
            if (request == null)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Request cannot be null.");
            }

            if (request.CompanyId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyNotFound, "CompanyId must be a positive integer.");
            }
            EnsureSameCompany(callerCompanyId, request.CompanyId);

            if (request.Amount <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Amount must be greater than 0.");
            }

            var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
            var company = await ValidateCompanyAsync(request.CompanyId).ConfigureAwait(false);
            EnsureCompanyIsActive(company);

            var successfulPayment = await _paymentRepository.GetSuccessfulByCompanyAsync(request.CompanyId).ConfigureAwait(false);
            if (successfulPayment != null)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.DuplicateSuccessPayment, "Company has already unlocked full feature.");
            }

            var latestPending = await _paymentRepository.GetLatestPendingByCompanyAsync(request.CompanyId).ConfigureAwait(false);
            if (latestPending != null)
            {
                return ToPaymentDto(latestPending);
            }

            var now = ToUnspecifiedUtc(DateTime.UtcNow);
            var payment = new CompanyPayment
            {
                CompanyId = request.CompanyId,
                PaymentStatus = "PENDING",
                Amount = request.Amount,
                PaymentMethod = paymentMethod,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _paymentRepository.CreateAsync(payment).ConfigureAwait(false);
            return ToPaymentDto(created);
        }

        public async Task<PaymentDto> MarkPaymentSuccessAsync(int paymentId, int callerCompanyId)
        {
            if (paymentId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Payment id must be a positive integer.");
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
            if (payment == null)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, $"Payment with id {paymentId} not found.");
            }
            EnsureSameCompany(callerCompanyId, payment.CompanyId);

            var company = await ValidateCompanyAsync(payment.CompanyId).ConfigureAwait(false);
            EnsureCompanyIsActive(company);

            if (payment.PaymentStatus == "SUCCESS")
            {
                return ToPaymentDto(payment);
            }

            if (payment.PaymentStatus == "FAILED")
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Cannot unlock from FAILED payment.");
            }

            if (payment.PaymentStatus != "PENDING")
            {
                _logger.LogError("PAY-EX-06 invalid payment status transition: paymentId={PaymentId}, currentStatus={CurrentStatus}", payment.Id, payment.PaymentStatus);
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentStatus, $"Cannot mark payment as success from status '{payment.PaymentStatus}'.");
            }

            var successfulPayment = await _paymentRepository.GetSuccessfulByCompanyAsync(payment.CompanyId).ConfigureAwait(false);
            if (successfulPayment != null && successfulPayment.Id != payment.Id)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.DuplicateSuccessPayment, "Company already has a SUCCESS payment.");
            }

            var now = ToUnspecifiedUtc(DateTime.UtcNow);
            payment.PaymentStatus = "SUCCESS";
            payment.PaidAt = now;
            payment.UpdatedAt = now;

            var updated = await _paymentRepository.UpdateAsync(payment).ConfigureAwait(false);
            return ToPaymentDto(updated);
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(int companyId, int callerCompanyId)
        {
            if (companyId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyNotFound, "CompanyId must be a positive integer.");
            }
            EnsureSameCompany(callerCompanyId, companyId);

            await ValidateCompanyAsync(companyId).ConfigureAwait(false);

            var successfulPayment = await _paymentRepository.GetSuccessfulByCompanyAsync(companyId).ConfigureAwait(false);
            if (successfulPayment != null)
            {
                return ToStatusResult(companyId, successfulPayment, true, "SUCCESS");
            }

            var latest = await _paymentRepository.GetLatestByCompanyAsync(companyId).ConfigureAwait(false);
            if (latest == null)
            {
                return new PaymentStatusResult(companyId, false, "NOT_PAID", null, null, null, null);
            }

            var normalizedStatus = NormalizePaymentStatus(latest.PaymentStatus);
            return ToStatusResult(companyId, latest, false, normalizedStatus);
        }

        public async Task CheckWriteAccessAsync(int companyId)
        {
            if (companyId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyNotFound, "CompanyId must be a positive integer.");
            }

            try
            {
                var company = await ValidateCompanyAsync(companyId).ConfigureAwait(false);
                EnsureCompanyIsActive(company);

                var successfulPayment = await _paymentRepository.GetSuccessfulByCompanyAsync(companyId).ConfigureAwait(false);
                if (successfulPayment != null)
                {
                    return;
                }

                var latest = await _paymentRepository.GetLatestByCompanyAsync(companyId).ConfigureAwait(false);
                if (latest == null)
                {
                    throw new BusinessRuleException(
                        PaymentExceptionCodes.PaymentRequired,
                        "Company has not completed payment. Write actions are blocked."
                    );
                }

                var status = NormalizePaymentStatus(latest.PaymentStatus);
                if (status == "PENDING")
                {
                    throw new BusinessRuleException(
                        PaymentExceptionCodes.PaymentPending,
                        "Payment is pending. Company remains in view-only mode."
                    );
                }

                if (status == "FAILED")
                {
                    throw new BusinessRuleException(
                        PaymentExceptionCodes.PaymentFailed,
                        "Payment failed. Please retry payment to unlock full feature."
                    );
                }

                _logger.LogError("PAY-EX-06 invalid payment status when checking write access: companyId={CompanyId}, status={Status}", companyId, latest.PaymentStatus);
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentStatus, "Invalid payment status.");
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PAY-EX-12 payment check failed for companyId={CompanyId}", companyId);
                throw new BusinessRuleException(
                    PaymentExceptionCodes.PaymentCheckFailed,
                    "Cannot verify payment status right now. Write actions are blocked for safety."
                );
            }
        }

        public async Task<MomoAtmPaymentUrlResult> CreateMomoAtmPaymentUrlAsync(int paymentId, string? orderInfo, int callerCompanyId)
        {
            if (paymentId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Payment id must be a positive integer.");
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
            if (payment == null)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, $"Payment with id {paymentId} not found.");
            }
            EnsureSameCompany(callerCompanyId, payment.CompanyId);

            var company = await ValidateCompanyAsync(payment.CompanyId).ConfigureAwait(false);
            EnsureCompanyIsActive(company);

            var method = NormalizePaymentMethod(payment.PaymentMethod);
            if (!string.Equals(method, "MOMO", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentProviderError, "This payment is not configured for MoMo.");
            }

            var status = NormalizePaymentStatus(payment.PaymentStatus);
            if (status == "SUCCESS")
            {
                throw new BusinessRuleException(PaymentExceptionCodes.DuplicateSuccessPayment, "Payment already completed successfully.");
            }

            if (status == "FAILED")
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Cannot create MoMo URL from FAILED payment. Please create a new payment.");
            }

            if (status != "PENDING")
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentStatus, $"Invalid payment status '{payment.PaymentStatus}'.");
            }

            if (payment.Amount < 1000 || payment.Amount > 50000000)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentProviderError, "MoMo ATM amount must be between 1,000 and 50,000,000 VND.");
            }

            var request = new MomoAtmGatewayCreateRequest(
                payment.Id,
                payment.Amount,
                string.IsNullOrWhiteSpace(orderInfo) ? $"Unlock full feature for company {payment.CompanyId}" : orderInfo.Trim()
            );

            MomoAtmGatewayCreateResult gatewayResult;
            try
            {
                gatewayResult = await _momoAtmGatewayService.CreatePaymentUrlAsync(request).ConfigureAwait(false);
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PAY-EX-10 MoMo ATM create URL failed: paymentId={PaymentId}", paymentId);
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentProviderError, "Cannot create MoMo ATM payment URL.");
            }

            payment.UpdatedAt = ToUnspecifiedUtc(DateTime.UtcNow);
            await _paymentRepository.UpdateAsync(payment).ConfigureAwait(false);

            return new MomoAtmPaymentUrlResult(
                payment.Id,
                status,
                gatewayResult.RequestId,
                gatewayResult.OrderId,
                gatewayResult.PayUrl
            );
        }

        public async Task<MomoAtmCallbackProcessResult> ProcessMomoAtmCallbackAsync(MomoAtmCallbackRequest request, bool isIpn)
        {
            if (request == null)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Callback request cannot be null.");
            }

            if (!_momoAtmGatewayService.ValidateCallbackSignature(request))
            {
                _logger.LogWarning("PAY-EX-11 invalid MoMo {Source} signature. orderId={OrderId}", isIpn ? "IPN" : "Callback", request.OrderId);
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentCallbackMismatch, "Invalid callback signature.");
            }

            if (!TryExtractPaymentId(request.OrderId, out var paymentId))
            {
                _logger.LogWarning("PAY-EX-11 cannot parse payment id from orderId {OrderId}", request.OrderId);
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentCallbackMismatch, "Invalid orderId from callback.");
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
            if (payment == null)
            {
                _logger.LogWarning("PAY-EX-11 payment not found for orderId {OrderId}", request.OrderId);
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentCallbackMismatch, "Payment not found for callback.");
            }

            if (!string.Equals(payment.PaymentMethod, "MOMO", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentCallbackMismatch, "Callback does not match payment method.");
            }

            if (!TryParseAmount(request.Amount, out var callbackAmount) || callbackAmount != payment.Amount)
            {
                _logger.LogWarning(
                    "PAY-EX-11 callback amount mismatch: paymentId={PaymentId}, expected={Expected}, actual={Actual}",
                    payment.Id,
                    payment.Amount,
                    request.Amount);
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentCallbackMismatch, "Callback amount mismatch.");
            }

            if (!int.TryParse(request.ResultCode, out var resultCode))
            {
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentCallbackMismatch, "Callback resultCode is invalid.");
            }

            var currentStatus = NormalizePaymentStatus(payment.PaymentStatus);
            if (resultCode == 0)
            {
                if (currentStatus == "FAILED")
                {
                    throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Invalid transition from FAILED to SUCCESS.");
                }

                if (currentStatus == "PENDING")
                {
                    var now = ToUnspecifiedUtc(DateTime.UtcNow);
                    payment.PaymentStatus = "SUCCESS";
                    payment.PaidAt = now;
                    payment.UpdatedAt = now;
                    await _paymentRepository.UpdateAsync(payment).ConfigureAwait(false);
                    currentStatus = "SUCCESS";
                }
            }
            else
            {
                if (currentStatus == "SUCCESS")
                {
                    throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentUpdate, "Invalid transition from SUCCESS to FAILED.");
                }

                if (currentStatus == "PENDING")
                {
                    payment.PaymentStatus = "FAILED";
                    payment.UpdatedAt = ToUnspecifiedUtc(DateTime.UtcNow);
                    await _paymentRepository.UpdateAsync(payment).ConfigureAwait(false);
                    currentStatus = "FAILED";
                }
            }

            return new MomoAtmCallbackProcessResult(
                payment.Id,
                payment.CompanyId,
                currentStatus,
                string.Equals(currentStatus, "SUCCESS", StringComparison.Ordinal),
                resultCode,
                request.Message ?? (resultCode == 0 ? "Success" : "Failed")
            );
        }

        private static string NormalizePaymentMethod(string? paymentMethod)
        {
            var normalized = paymentMethod?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized) || !AllowedMethods.Contains(normalized))
            {
                throw new BusinessRuleException(PaymentExceptionCodes.PaymentProviderError, "PaymentMethod must be one of: MANUAL, MOMO, VNPAY.");
            }

            return normalized;
        }

        private static string NormalizePaymentStatus(string? paymentStatus)
        {
            var normalized = paymentStatus?.Trim().ToUpperInvariant();
            return normalized switch
            {
                "SUCCESS" => "SUCCESS",
                "FAILED" => "FAILED",
                "PENDING" => "PENDING",
                _ => throw new BusinessRuleException(PaymentExceptionCodes.InvalidPaymentStatus, $"Invalid payment status '{paymentStatus}'.")
            };
        }

        private async Task<Company> ValidateCompanyAsync(int companyId)
        {
            var company = await _paymentRepository.GetCompanyByIdAsync(companyId).ConfigureAwait(false);
            if (company == null)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyNotFound, $"Company with id {companyId} not found.");
            }

            return company;
        }

        private static void EnsureCompanyIsActive(Company company)
        {
            if (string.Equals(company.Status, "DEACTIVATED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(company.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyInactive, "Company is inactive and cannot process payment unlock.");
            }
        }

        private static DateTime ToUnspecifiedUtc(DateTime utcNow)
        {
            return DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified);
        }

        private static PaymentDto ToPaymentDto(CompanyPayment payment)
        {
            return new PaymentDto(
                payment.Id,
                payment.CompanyId,
                NormalizePaymentStatus(payment.PaymentStatus),
                payment.Amount,
                payment.PaymentMethod,
                payment.PaidAt,
                payment.CreatedAt,
                payment.UpdatedAt
            );
        }

        private static PaymentStatusResult ToStatusResult(int companyId, CompanyPayment payment, bool isUnlocked, string paymentStatus)
        {
            return new PaymentStatusResult(
                companyId,
                isUnlocked,
                paymentStatus,
                payment.Id,
                payment.Amount,
                payment.PaymentMethod,
                payment.PaidAt
            );
        }

        private static bool TryExtractPaymentId(string? orderId, out int paymentId)
        {
            paymentId = 0;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            var parts = orderId.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 3 || !string.Equals(parts[0], "PAY", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out paymentId) && paymentId > 0;
        }

        private static bool TryParseAmount(string? amountRaw, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(amountRaw))
            {
                return false;
            }

            return decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        private static void EnsureSameCompany(int callerCompanyId, int targetCompanyId)
        {
            if (callerCompanyId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyNotFound, "CompanyId in token is invalid.");
            }

            if (targetCompanyId <= 0)
            {
                throw new BusinessRuleException(PaymentExceptionCodes.CompanyNotFound, "CompanyId must be a positive integer.");
            }

            if (callerCompanyId != targetCompanyId)
            {
                throw new BusinessRuleException(
                    PaymentExceptionCodes.CrossCompanyAccessDenied,
                    "Khong cung cong ty. Ban khong the thao tac payment cua cong ty khac."
                );
            }
        }
    }
}
