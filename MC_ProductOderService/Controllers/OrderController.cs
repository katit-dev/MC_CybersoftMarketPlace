using Microsoft.AspNetCore.Mvc;

namespace MC_ProductOderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        public OrderController()
        {
        }

        [HttpGet("get-order")]
        public IActionResult GetOrder()
        {
            return Ok(new
            {
                OrderId = 1,
                ProductName = "Sample Product",
                Quantity = 2
            });
        }
    }
}
