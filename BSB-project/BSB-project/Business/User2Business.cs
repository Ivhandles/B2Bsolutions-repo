using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BSB_project.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace BSB_project.Business
{
    public class User2Business
    {
        private readonly string connectionString;

        public User2Business(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<bool> UploadUserJsonAsync(Stream dataStream, string containerName, string blobName)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.UploadAsync(dataStream, true);
                Console.WriteLine($"Data uploaded successfully to blob storage.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading data to blob storage: {ex.Message}");
                return false;
            }
        }
    }
}


