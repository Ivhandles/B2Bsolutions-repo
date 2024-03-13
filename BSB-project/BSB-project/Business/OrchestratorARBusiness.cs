using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using BSB_project.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace BSB_project.Business
{
    public class OrchestratorARBusiness
    {
        private readonly string connectionString;

        public OrchestratorARBusiness(string connectionString)
        {
            this.connectionString = connectionString;
        }
       
        public async Task<List<Initialjsonstruct>> GetExistingDataFromBlobStorageAsync(string blobContainerName, string blobName)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        await blobClient.DownloadToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        using (var reader = new StreamReader(stream))
                        {
                            var content = await reader.ReadToEndAsync();
                            var userList = JsonConvert.DeserializeObject<List<Initialjsonstruct>>(content);


                            return userList;
                        }
                    }
                }
                else
                {
                    // Blob doesn't exist, return an empty list or handle as needed
                    Console.WriteLine($"Blob with name '{blobName}' does not exist in container '{blobContainerName}'. Returning an empty list.");
                    if (!await containerClient.ExistsAsync())
                    {
                        await containerClient.CreateIfNotExistsAsync();
                        Console.WriteLine($"Container '{blobContainerName}' created successfully.");
                    }

                    // Create the blob
                    await blobClient.UploadAsync(new MemoryStream(), true);
                    Console.WriteLine($"Blob '{blobName}' created successfully in container '{blobContainerName}'.");

                    return new List<Initialjsonstruct>();
                }
            }
            catch (RequestFailedException requestEx)
            {
                Console.WriteLine($"Azure Storage request failed: {requestEx.Message}");
                throw;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error deserializing JSON: {jsonEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UploadDataToBlobStorageAsync(string containerName, string blobName, string jsonData)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                byte[] bytes = Encoding.UTF8.GetBytes(jsonData);

                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    await blobClient.UploadAsync(stream, true);
                    Console.WriteLine($"Data uploaded successfully to blob storage.");
                    return true;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading data to blob storage: {ex.Message}");
                return false;
            }
        }
    
    }
}