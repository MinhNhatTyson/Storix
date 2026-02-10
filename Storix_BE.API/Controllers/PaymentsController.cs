using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storix_BE.Domain.Exception;
using Storix_BE.Service.Interfaces;
using System.Security.Claims;

namespace Storix_BE.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request cannot be null." });
            }
            var callerCompanyId = GetCompanyIdFromToken();
            if (!callerCompanyId.HasValue)
            {
                return Unauthorized(new { message = "Missing CompanyId in token." });
            }

            try
            {
                var payment = await _paymentService.CreatePaymentAsync(request, callerCompanyId.Value);
                return Ok(payment);
            }
            catch (BusinessRuleException ex)
            {
                return MapPaymentException(ex);
            }
        }

        [HttpPut("{id:int}/success")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> MarkPaymentSuccess(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid payment id." });
            }
            var callerCompanyId = GetCompanyIdFromToken();
            if (!callerCompanyId.HasValue)
            {
                return Unauthorized(new { message = "Missing CompanyId in token." });
            }

            try
            {
                var payment = await _paymentService.MarkPaymentSuccessAsync(id, callerCompanyId.Value);
                return Ok(payment);
            }
            catch (BusinessRuleException ex)
            {
                return MapPaymentException(ex);
            }
        }

        [HttpGet("status")]
        [Authorize(Roles = "2,3")]
        public async Task<IActionResult> GetPaymentStatus([FromQuery(Name = "company_id")] int companyId)
        {
            if (companyId <= 0)
            {
                return BadRequest(new { message = "company_id must be a positive integer." });
            }
            var callerCompanyId = GetCompanyIdFromToken();
            if (!callerCompanyId.HasValue)
            {
                return Unauthorized(new { message = "Missing CompanyId in token." });
            }

            try
            {
                var status = await _paymentService.GetPaymentStatusAsync(companyId, callerCompanyId.Value);
                return Ok(status);
            }
            catch (BusinessRuleException ex)
            {
                return MapPaymentException(ex);
            }
        }

        [HttpPost("{id:int}/momo/atm-url")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> CreateMomoAtmPaymentUrl(int id, [FromBody] CreateMomoAtmUrlRequest? request)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid payment id." });
            }
            var callerCompanyId = GetCompanyIdFromToken();
            if (!callerCompanyId.HasValue)
            {
                return Unauthorized(new { message = "Missing CompanyId in token." });
            }

            try
            {
                var result = await _paymentService.CreateMomoAtmPaymentUrlAsync(id, request?.OrderInfo, callerCompanyId.Value);
                return Ok(result);
            }
            catch (BusinessRuleException ex)
            {
                return MapPaymentException(ex);
            }
        }

        [HttpGet("momo/atm/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleMomoAtmCallback([FromQuery] MomoAtmCallbackRequest request)
        {
            try
            {
                var result = await _paymentService.ProcessMomoAtmCallbackAsync(request, false);
                return Ok(result);
            }
            catch (BusinessRuleException ex)
            {
                return MapPaymentException(ex);
            }
        }

        [HttpPost("momo/atm/ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleMomoAtmIpn([FromBody] MomoAtmCallbackRequest request)
        {
            try
            {
                var result = await _paymentService.ProcessMomoAtmCallbackAsync(request, true);
                return Ok(new { resultCode = 0, message = "Success", paymentStatus = result.PaymentStatus });
            }
            catch (BusinessRuleException ex)
            {
                return Ok(new { resultCode = 1, message = ex.Message, code = ex.Code });
            }
        }

        private IActionResult MapPaymentException(BusinessRuleException ex)
        {
            var payload = new { code = ex.Code, message = ex.Message };
            return ex.Code switch
            {
                PaymentExceptionCodes.PaymentRequired => StatusCode(StatusCodes.Status402PaymentRequired, payload),
                PaymentExceptionCodes.ViewOnlyAccess => StatusCode(StatusCodes.Status402PaymentRequired, payload),
                PaymentExceptionCodes.PaymentPending => StatusCode(StatusCodes.Status402PaymentRequired, payload),
                PaymentExceptionCodes.PaymentFailed => StatusCode(StatusCodes.Status402PaymentRequired, payload),
                PaymentExceptionCodes.DuplicateSuccessPayment => Conflict(payload),
                PaymentExceptionCodes.CompanyNotFound => NotFound(payload),
                PaymentExceptionCodes.CompanyInactive => Conflict(payload),
                PaymentExceptionCodes.PaymentProviderError => BadRequest(payload),
                PaymentExceptionCodes.InvalidPaymentStatus => BadRequest(payload),
                PaymentExceptionCodes.InvalidPaymentUpdate => Conflict(payload),
                PaymentExceptionCodes.CrossCompanyAccessDenied => StatusCode(StatusCodes.Status403Forbidden, payload),
                PaymentExceptionCodes.PaymentCallbackMismatch => BadRequest(payload),
                PaymentExceptionCodes.PaymentCheckFailed => StatusCode(StatusCodes.Status503ServiceUnavailable, payload),
                _ => BadRequest(payload)
            };
        }

        private int? GetCompanyIdFromToken()
        {
            var companyIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrWhiteSpace(companyIdStr))
            {
                companyIdStr = User.FindFirst(ClaimTypes.GroupSid)?.Value;
            }
            return int.TryParse(companyIdStr, out var companyId) ? companyId : null;
        }
    }
}
