using Microsoft.AspNetCore.Mvc;

namespace MC_ProductOderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        public ProductController()
        {
        }

        [HttpGet("get-product")]
        public IActionResult GetProduct()
        {
            return Ok(new
            {
                ProductId = 1,
                ProductName = "Sample Product",
                Price = 100
            });
        }
    }
}
