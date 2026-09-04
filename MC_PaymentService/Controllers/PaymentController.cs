using Microsoft.AspNetCore.Mvc;

namespace MC_PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        public PaymentController()
        {
        }

        [HttpGet("get-payment")]
        public IActionResult GetPayment()
        {
            return Ok(new
            {
                PaymentId = 1,
                Amount = 100
            });
        }
    }
}
