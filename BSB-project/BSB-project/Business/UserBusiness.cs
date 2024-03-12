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
        private const string blobcontainerName = "amdox-container";
        private const string blobName = "initialdb.json";
        //method to process the postedjsonfile
        public async Task PostJsonfile(IFormFile file)
        {
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


                    await UpdateorInsertBlobData(userList, blobData);

                    var result = UploadtoBlob(blobData);
                    var pollingblobData = await ReadJsonFromBlobAsync();

                    List<Initialjsonstruct> unsyncedList = pollingblobData.Where(item => !item.IsSynced).ToList();

                    var eventStatus = PostEvent(unsyncedList);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error processing JSON file.", ex);
            }
        }

        //Method to check whether the user is to be updated or added to the blob storage
        public async Task UpdateorInsertBlobData(List<Initialjsonstruct> userList, List<Initialjsonstruct> blobData)
        {
            foreach (var user in userList)
            {
                var existingUserIndex = blobData.FindIndex(u => u.UserGuid == user.UserGuid);

                if (existingUserIndex != -1)
                {
                    if (!AreUsersEqual(user, blobData[existingUserIndex]))
                    {

                        blobData[existingUserIndex] = user;
                    }
                }
                else
                {

                    blobData.Add(user);
                }
            }
        }
        //method to compare the userlist and the data from the blob
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



        //method to read the data from blob
        public async Task<List<Initialjsonstruct>> ReadJsonFromBlobAsync()
        {
            try
            {


                BlobServiceClient blobServiceClient = new BlobServiceClient(blobconnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobcontainerName);
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



                            return fetcheduserList;
                        }
                    }
                }
                else
                {
                    return new List<Initialjsonstruct>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data from blob storage: {ex.Message}");
                throw;
            }
        }

        //method to post the unsyncedlist to the event
        public async Task PostEvent(List<Initialjsonstruct> userList)
        {
            EventHubProducerClient producerClient = null;
            List<Azure.Messaging.EventHubs.EventData> eventsToSend = new List<Azure.Messaging.EventHubs.EventData>();

            try
            {
                producerClient = new EventHubProducerClient(connectionString, eventHubName);

                foreach (var user in userList)
                {
                    string jsonUser = JsonConvert.SerializeObject(user);


                    byte[] eventDataBytes = Encoding.UTF8.GetBytes(jsonUser);


                    Azure.Messaging.EventHubs.EventData eventData = new Azure.Messaging.EventHubs.EventData(eventDataBytes);
                    eventsToSend.Add(eventData);
                }


                await producerClient.SendAsync(eventsToSend.ToArray());
                await producerClient.DisposeAsync();
                Console.WriteLine($"Successfully sent {eventsToSend.Count} events to Event Hub.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending events: {ex.Message}");
            }

        }

        //method to upload the data to blob
        private async Task UploadtoBlob(List<Initialjsonstruct> userList)
        {

            string jsonData = JsonConvert.SerializeObject(userList);



            BlobServiceClient blobServiceClient = new BlobServiceClient(blobconnectionString);


            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobcontainerName);




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