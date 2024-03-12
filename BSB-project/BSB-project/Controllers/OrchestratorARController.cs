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

        [HttpPost("uploadUserORToAR")]
        public async Task<IActionResult> UploadJsonData([FromBody] Initialjsonstruct newDataItem)
        {
            if (newDataItem == null)
                return BadRequest("No JSON data uploaded.");


            try
            {
                string blobName = "orchestratorAR.json";
                var existingData = await orchestratorARBusiness.GetExistingDataFromBlobStorageAsync("amdox-container", blobName);

                await UpdateorInsertBlobData(newDataItem, existingData);

                string updatedDataJson = JsonConvert.SerializeObject(existingData);

                bool uploadSuccess = await orchestratorARBusiness.UploadDataToBlobStorageAsync("amdox-container", blobName, updatedDataJson);

                if (!uploadSuccess)
                {
                    Console.WriteLine($"Error: Data could not be Updated to AR db.");
                    var responseError = new Dictionary<string, string>
                        {
                            { $"{newDataItem.UserGuid}", "Failed" }
                        };
              
                    return StatusCode(500, responseError);
                }
                else
                {
                    
                    var response = new Dictionary<string, string>
                    {
                        { $"{newDataItem.UserGuid}", "Success" }
                    };
                    Console.WriteLine($"Object with UserGuid {newDataItem.UserGuid} updated In AR database");
                    Console.WriteLine("Api ended");
                    return Ok(response);
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
           
        }


        public bool AreUsersEqual(Initialjsonstruct user1, Initialjsonstruct user2)
        {
            return user1.UserGuid == user2.UserGuid &&
                   user1.B2BUserld == user2.B2BUserld &&
                   user1.B2BAccessCode == user2.B2BAccessCode &&
                   user1.UttUID == user2.UttUID &&
                   user1.Type == user2.Type &&
                   user1.UserName == user2.UserName &&
                   user1.FirstName == user2.FirstName &&
                   user1.LastName == user2.LastName &&
                   user1.FullName == user2.FullName &&
                   user1.Email == user2.Email &&
                   user1.IsSynced == user2.IsSynced &&
                   user1.SyncStatus == user2.SyncStatus &&
                   user1.ModificationDate == user2.ModificationDate &&
                   user1.ModificationBatch == user2.ModificationBatch &&
                   user1.SyncDate == user2.SyncDate;
        }

        public async Task UpdateorInsertBlobData(Initialjsonstruct user, List<Initialjsonstruct> blobData)
        {
          
            
                var existingUserIndex = blobData.FindIndex(u => u.UserGuid == user.UserGuid);
            var existingIsSyncedIndex = blobData.FindIndex(u => u.IsSynced == user.IsSynced);

            if (existingUserIndex != -1)
                {
                    if (!AreUsersEqual(user, blobData[existingUserIndex]))
                    {

                        blobData[existingUserIndex] = user;
                    blobData[existingIsSyncedIndex].IsSynced = true;

                }
            }
                else
                {
              
                blobData.Add(user);
                var existingIsSyncedIndexnew = blobData.FindIndex(u => u.IsSynced == user.IsSynced);
                blobData[existingIsSyncedIndexnew].IsSynced = true;


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
                        Console.WriteLine($"Data successfully stored in Azure Blob Storage. AR database");
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



        [HttpPost("uploadDealersorLocationDataa")]
        public async Task<IActionResult> UploadFile([FromQuery] string dataType, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (dataType != "Dealers" && dataType != "Locations")
                return BadRequest("Invalid dataType. Supported values are 'Dealers' or 'Locations'.");

            try
            {
                string originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                string blobName = $"{originalFileName}";
                // Read the content of the uploaded file
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    var content = await reader.ReadToEndAsync();
                    var jsonData = JsonConvert.DeserializeObject<List<Initialjsonstruct>>(content);

                    UserDataList.UserData = jsonData;
                    string userListJson = JsonConvert.SerializeObject(UserDataList.UserData);
                    bool uploadSuccess = await orchestratorARBusiness.UploadDataToBlobStorageAsync("container", blobName, userListJson);

                    if (uploadSuccess)
                    {
                        // Display success message
                        Console.WriteLine($"Data successfully stored in Azure Blob Storage.AR db");
                    }
                    else
                    {
                        // Display error message
                        Console.WriteLine($"Error: Data could not be stored in Azure Blob Storage.");
                    }
                    // Return the stored User data
                    return Ok(UserDataList.UserData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}