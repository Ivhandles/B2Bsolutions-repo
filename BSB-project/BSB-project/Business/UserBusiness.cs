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
                    //userlisttoupdate = polling function , it need get db content where issynced is false 

                    
                    

                    var eventStatus = PostEvent(userList);
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error processing JSON file.", ex);
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
