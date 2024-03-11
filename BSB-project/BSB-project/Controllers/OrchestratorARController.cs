﻿using BSB_project.Business;
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
        public async Task<IActionResult> UploadJsonData([FromBody] List<Initialjsonstruct> jsonData)
        {
            if (jsonData == null || jsonData.Count == 0)
                return BadRequest("No JSON data uploaded.");

            try
            {
                string blobName = "orchestratorAR.json";
                // Read the content of the uploaded file
                //var jsonData = JsonConvert.DeserializeObject<List<Initialjsonstruct>>(jsonString);

                // Fetch existing data from blob storage
                var existingData = await orchestratorARBusiness.GetExistingDataFromBlobStorageAsync("amdox-container", blobName);


                    // Update existing objects or add new ones
                    foreach (var newDataItem in jsonData)
                    {
                        var existingItem = existingData.FirstOrDefault(item => item.UserGuid == newDataItem.UserGuid);

                        if (existingItem != null)
                        {
                            // Update existing item with newDataItem values
                            existingItem.UserGuid = newDataItem.UserGuid;
                            existingItem.B2BUserld = newDataItem.B2BUserld;
                            existingItem.FullName = newDataItem.FullName;
                            existingItem.Type = newDataItem.Type;
                            existingItem.UttUID = newDataItem.UttUID;
                            existingItem.FirstName = newDataItem.FirstName;
                            existingItem.LastName = newDataItem.LastName;
                            existingItem.B2BAccessCode = newDataItem.B2BAccessCode;
                            existingItem.Email = newDataItem.Email;
                            existingItem.SyncDate = newDataItem.SyncDate;
                            existingItem.UserName = newDataItem.UserName;
                            existingItem.ModificationBatch = newDataItem.ModificationBatch;
                            existingItem.ModificationDate = newDataItem.ModificationDate;
                            existingItem.SyncStatus = newDataItem.SyncStatus;
                            existingItem.IsSynced = true;
                        }
                        else
                        {
                            // Add new item to the existing data
                            existingData.Add(newDataItem);
                        }
                    }
                Dictionary<Guid, string> userStatusList = new Dictionary<Guid, string>();

                // Add some sample data
                userStatusList.Add(Guid.NewGuid(), "Updated");
                userStatusList.Add(Guid.NewGuid(), "Not updated");
                userStatusList.Add(Guid.NewGuid(), "Updated");
                userStatusList.Add(Guid.NewGuid(), "Updated");
                userStatusList.Add(Guid.NewGuid(), "Updated");

                // Serialize the updated data
                string updatedDataJson = JsonConvert.SerializeObject(existingData);

                    // Upload the updated data to blob storage
                    bool uploadSuccess = await orchestratorARBusiness.UploadDataToBlobStorageAsync("amdox-container", blobName, updatedDataJson);

                    if (uploadSuccess)
                    {
                        // Display success message
                        Console.WriteLine($"Data successfully stored in Azure Blob Storage.");
                        return Ok(userStatusList);

                    }
                    else
                    {
                        // Display error message
                        Console.WriteLine($"Error: Data could not be stored in Azure Blob Storage.");
                        return StatusCode(500, "Internal Server Error");

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
                        Console.WriteLine($"Data successfully stored in Azure Blob Storage.");
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