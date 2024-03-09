using BSB_project.Business;
using BSB_project.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace BSB_project.Controllers
{
    public class OrchestratorARController : Controller
    {

        private readonly OrchestratorARBusiness orchestratorARBusiness;

        public OrchestratorARController()
        {

            string connectionString = "DefaultEndpointsProtocol=https;AccountName=amdox;AccountKey=EsOwsWTExYkxhSDuyhUJ1Ls0yCLjKI/ULQo92BGPXs2xgyy0nQsOCqwRdY3g9FKAogOFGYV6xrzH+AStDwsqaw==;EndpointSuffix=core.windows.net";
            this.orchestratorARBusiness = new OrchestratorARBusiness(connectionString);
        }

        [HttpPost("uploadUserData")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadSuccess = await orchestratorARBusiness.UploadUserJsonAsync(stream, "amdox-container", Path.GetFileNameWithoutExtension(file.FileName));
                    if (uploadSuccess)
                    {
                        Console.WriteLine($"Data successfully stored in Azure Blob Storage.");
                        return Ok("File uploaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: Data could not be stored in Azure Blob Storage.");
                        return StatusCode(500, "Internal Server Error");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("uploadDealersorLocationData")]
        public async Task<IActionResult> UploadFileForDealersOrLocations(IFormFile file, string uploadType)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (string.IsNullOrEmpty(uploadType) || (uploadType != "Dealers" && uploadType != "Locations"))
                return BadRequest("Invalid upload type. Please select Dealers or Locations.");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadSuccess = await orchestratorARBusiness.UploadUserJsonAsync(stream, "amdox-container", Path.GetFileNameWithoutExtension(file.FileName));
                    if (uploadSuccess)
                    {
                        Console.WriteLine($"Data successfully stored in Azure Blob Storage.");
                        string fileUploadedMessage;
                        switch (uploadType)
                        {
                            case "Dealers":
                                fileUploadedMessage = "Dealers File uploaded successfully.";
                                break;
                            case "Locations":
                                fileUploadedMessage = "Locations File uploaded successfully.";
                                break;
                            default:
                                fileUploadedMessage = "Invalid upload type. Please select Dealers or Locations. "; 
                                break;
                        }
                        return Ok(fileUploadedMessage);
    
                }
                    else
                    {
                        Console.WriteLine($"Error: Data could not be stored in Azure Blob Storage.");
                        return StatusCode(500, "Internal Server Error");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
       }
    }
}