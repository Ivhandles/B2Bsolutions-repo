using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BSB_project.Model;
using System.Text.Json;
using Newtonsoft.Json;
using Azure.Storage.Blobs;

namespace BSB_project.Business
{
    public class UserBusiness
    {
        public async Task PostJsonfile(IFormFile file)
        {
            // Check if the file is not null and has content
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty.");
            }

            try
            {
                using (var streamReader = new StreamReader(file.OpenReadStream()))
                {
                    var jsonContent = await streamReader.ReadToEndAsync();

                    
                    var userList = System.Text.Json.JsonSerializer.Deserialize<List<Initialjsonstruct>>(jsonContent);
                    var result = UploadtoBlob(userList);
                     


                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error processing JSON file.", ex);
            }
        }

      private   async Task  UploadtoBlob(List<Initialjsonstruct> userList)
        {
            string jsonData = JsonConvert.SerializeObject(userList);

           
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=amdox;AccountKey=43S9I/6JTqmdyeHJnB/85+KQg0f1Zrm+tLfyHj/gbjegG5HIWSfwm5hE4dha353O74nh6iZ73WAW+AStE/hxUw==;EndpointSuffix=core.windows.net";

           
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

           
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("amdox-container");

           
            string blobName = "initialdb.json";

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                await blobClient.UploadAsync(stream, true);
                Console.WriteLine($"Data uploaded successfully to blob storage.");
            }
        }
        
        
    }
}
