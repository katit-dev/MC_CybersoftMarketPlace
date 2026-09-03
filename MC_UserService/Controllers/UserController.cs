using Microsoft.AspNetCore.Mvc;

namespace MC_UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        public UserController()
        {
        }

        [HttpGet("get-user")]
        public IActionResult GetUser()
        {
            return Ok(new
            {
                Name = "John Doe",
                Email = "john.doe@example.com"
            });
        }
    }
}
