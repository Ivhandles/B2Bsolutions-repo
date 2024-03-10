using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BSB_project.Model;
using System.Text.Json;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Text;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;


namespace BSB_project.Business
{
    public class UserBusiness
    {
        private const string connectionString = "Endpoint=sb://amdocs-b2b.servicebus.windows.net/;SharedAccessKeyName=amdox-eventhub;SharedAccessKey=VWZ6DMYNQvbRSSuRIghlyg36XzXWdNC72+AEhFaXWGw=;EntityPath=amdox-eventhub";
        private const string eventHubName = "amdox-eventhub";
       private const string blobconnectionString = "DefaultEndpointsProtocol=https;AccountName=amdox;AccountKey=EsOwsWTExYkxhSDuyhUJ1Ls0yCLjKI/ULQo92BGPXs2xgyy0nQsOCqwRdY3g9FKAogOFGYV6xrzH+AStDwsqaw==;EndpointSuffix=core.windows.net";

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

                    var blobData = await ReadJsonFromBlobAsync();

                    foreach (var user in userList)
                    {
                        var existingUserIndex = blobData.FindIndex(u => u.UserGuid == user.UserGuid);

                        if (existingUserIndex != -1)
                        {
                            // Update existing user in blobData if data is different
                            if (!AreUsersEqual(user, blobData[existingUserIndex]))
                            {
                                // Update user data in blobData
                                blobData[existingUserIndex] = user;
                            }
                        }
                        else
                        {
                            // If user ID is new, append to the blobData
                            blobData.Add(user);
                        }
                    }

                    var result = UploadtoBlob(blobData);


                    // Additional processing if needed

                    var eventStatus = PostEvent(userList);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error processing JSON file.", ex);
            }
        }
        // Helper method to check if two users are equal
private bool AreUsersEqual(Initialjsonstruct user1, Initialjsonstruct user2)
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

// Helper method to update user properties
private void UpdateUser(Initialjsonstruct existingUser, Initialjsonstruct newUser)
{
    existingUser.UserGuid = newUser.UserGuid;
    existingUser.B2BUserld = newUser.B2BUserld;
    existingUser.B2BAccessCode = newUser.B2BAccessCode;
    existingUser.UttUID = newUser.UttUID;
    existingUser.Type = newUser.Type;
    existingUser.UserName = newUser.UserName;
    existingUser.FirstName = newUser.FirstName;
    existingUser.LastName = newUser.LastName;
    existingUser.FullName = newUser.FullName;
    existingUser.Email = newUser.Email;
    existingUser.IsSynced = newUser.IsSynced;
    existingUser.SyncStatus = newUser.SyncStatus;
    existingUser.ModificationDate = newUser.ModificationDate;
    existingUser.ModificationBatch = newUser.ModificationBatch;
    existingUser.SyncDate = newUser.SyncDate;
}

        public async Task<List<Initialjsonstruct>> ReadJsonFromBlobAsync()
        {
            try
            {
                string containerName = "amdox-container";
                string blobName = "initialdb.json";

                BlobServiceClient blobServiceClient = new BlobServiceClient(blobconnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
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
                            var fetcheduserList = JsonConvert.DeserializeObject<List<Initialjsonstruct>>(content);

                            // Log the retrieved user list
                            Console.WriteLine($"Successfully retrieved user list from blob storage. Count: {fetcheduserList.Count}");

                            return fetcheduserList;
                        }
                    }
                }
                else
                {
                    // Blob doesn't exist, return an empty list or handle as needed
                    Console.WriteLine($"Blob with name '{blobName}' does not exist in container '{containerName}'. Returning an empty list.");
                    return new List<Initialjsonstruct>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data from blob storage: {ex.Message}");
                throw; // Handle or log the exception as needed
            }
        }


        public async Task PostEvent(List<Initialjsonstruct> userList)
        {
            EventHubProducerClient producerClient = null;
            List<Azure.Messaging.EventHubs.EventData> eventsToSend = new List<Azure.Messaging.EventHubs.EventData>();

            try
            {
                producerClient = new EventHubProducerClient(connectionString, eventHubName);

                foreach (var user in userList)
                {
                    // Serialize the user object to JSON (replace with your actual serialization logic)
                    string jsonUser = JsonConvert.SerializeObject(user);

                    // Convert the JSON string to bytes
                    byte[] eventDataBytes = Encoding.UTF8.GetBytes(jsonUser);

                    // Create EventData with binary data
                    Azure.Messaging.EventHubs.EventData eventData = new Azure.Messaging.EventHubs.EventData(eventDataBytes);
                    eventsToSend.Add(eventData);
                }

                // Send the collection of events in batches for efficiency
                await producerClient.SendAsync(eventsToSend.ToArray());
                await producerClient.DisposeAsync();
                Console.WriteLine($"Successfully sent {eventsToSend.Count} events to Event Hub.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending events: {ex.Message}");
                // Log the error for further investigation and potential retries
            }
          
        }
        private async Task  UploadtoBlob(List<Initialjsonstruct> userList)
        {

           // DBLIst=get blobinistaildb contents
          // 
            string jsonData = JsonConvert.SerializeObject(userList);

           
           

           
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobconnectionString);

           
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
