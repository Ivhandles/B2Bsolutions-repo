using BSB_project.Business;
using Microsoft.AspNetCore.Mvc;

namespace BSB_project.Controllers
{

    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserBusiness userBusiness;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
            userBusiness = new UserBusiness();
        }
        [HttpPost("PostJson")]
        public async Task<IActionResult> PostJsonfile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File not selected or empty.");
            }

            try
            {
               
                await userBusiness.PostJsonfile(file);

                return Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error processing JSON file.");
               
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


    }
}
