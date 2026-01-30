using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storix_BE.Service.Interfaces;

namespace Storix_BE.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }
        [HttpGet("get-all/{companyId:int}")]
        public async Task<IActionResult> GetAllProductsFromACompany(int companyid)
        {
            var items = await _service.GetByCompanyAsync(companyid);
            return Ok(items);
        }
        [HttpGet("get-by-id/{companyId:int}/{id:int}")]
        public async Task<IActionResult> GetById(int companyId, int id)
        {
            var item = await _service.GetByIdAsync(companyId, id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("get-by-sku/{companyId:int}/sku/{sku}")]
        public async Task<IActionResult> GetBySku(int companyId, string sku)
        {
            var item = await _service.GetBySkuAsync(sku, companyId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] Storix_BE.Service.Interfaces.CreateProductRequest request)
        {
            try
            {
                var product = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { companyId = product.CompanyId, id = product.Id }, product);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("update{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Storix_BE.Service.Interfaces.UpdateProductRequest request)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("company/{companyId:int}/{id:int}")]
        public async Task<IActionResult> Delete(int companyId, int id)
        {
            var deleted = await _service.DeleteAsync(companyId, id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
