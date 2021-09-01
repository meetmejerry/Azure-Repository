using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.ServiceBus;
using Azure.Messaging.ServiceBus;

namespace printName
{
    public class printName
    {
        private const string V = "printName";
        private TelemetryClient telemetryClient;


        // Using dependency injection will guarantee that 
        // you use the same configuration for telemetry collected automatically and manually.
        public printName(TelemetryConfiguration telemetryConfiguration)
        {
            //get Telemetry Client Configuration
            telemetryClient = TelemetryFactory.GetTelemetryClient();
        
    }

        [FunctionName(V)]
        
        //this line means we are mentioning output bindings.
        //We pass the queue name and then the entity(queue) to bind
        //[return: ServiceBus("demoqueue", EntityType.Queue)]
        public async Task<IActionResult> Run(
        //here we changed the return type to string as we are sending a message to the 
        //message queue.
        //public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request by Joel.");


            string name = req.Query["name"];
            string type = req.Query["type"];
            //log.LogInformation("Initiating the send message to queue");
            //string queue_message = string.Empty;


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            type = type ?? data.type;
            log.LogInformation("The name we received is "+name);
            log.LogInformation("The type we received is " + type);
            if (name.Equals("joel"))
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("nameParameter", name);
                telemetryClient.TrackEvent("Joel is passed", dictionary);
                telemetryClient.TrackTrace("Joel has been passed as arguement");
                //queue_message = "Sending Joel as a message to the queue";
            }
            else 
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("nameParameter", name);
                telemetryClient.TrackEvent(name+ " Name is passed", dictionary);
                telemetryClient.TrackTrace(name+ " is passed as argument");
                //queue_message = "Sending " + name + " as a message to the queue";
            }
            log.LogInformation("Response String for Message has been set successfully");


            //starting the message sending code.

            string connection_string = 
                "Endpoint=sb://joelservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=BKpk29NbbFOC27yxFOfdfPJtjYqkZNuS+BijOl2+JOQ=";

            int NumofMessages = 5;
            switch (type)
            {
                case "queue":
                    {
                        string queue_name = "demoqueue";
                        ServiceBusClient queue_client;
                        ServiceBusSender queue_sender;

                        queue_client = new ServiceBusClient(connection_string);
                        queue_sender = queue_client.CreateSender(queue_name);

                        //create a batch of message 
                        using ServiceBusMessageBatch queue_messageBatch =
                            await queue_sender.CreateMessageBatchAsync();


                        for (int i = 0; i < NumofMessages; i++)
                        {
                            if (!queue_messageBatch.TryAddMessage(new ServiceBusMessage($"Queue Message {name} {i}")))
                            {
                                // if it is too large for the batch
                                throw new Exception($"The Queue message Message {name} {i} is too large to fit in the batch.");
                            }
                        }

                        try
                        {
                            await queue_sender.SendMessagesAsync(queue_messageBatch);
                            Console.WriteLine($"A batch of {NumofMessages} messages has been published to the queue.");
                        }
                        finally
                        {
                            await queue_sender.DisposeAsync();
                            await queue_client.DisposeAsync();
                        }
                    }
                    break;
                case "topic":
                    {
                        
                        string topic_name = "demotopic";


                        ServiceBusClient topic_client;
                        ServiceBusSender topic_sender;

                        

                        topic_client = new ServiceBusClient(connection_string);
                        topic_sender = topic_client.CreateSender(topic_name);

                        //create a batch of message 


                        using ServiceBusMessageBatch topic_messageBatch =
                            await topic_sender.CreateMessageBatchAsync();

                        for (int i = 0; i < NumofMessages; i++)
                        {
                            if (!topic_messageBatch.TryAddMessage(new ServiceBusMessage($"Topic Message {name} {i}")))
                            {
                                // if it is too large for the batch
                                throw new Exception($"The Topic message Message {name} {i} is too large to fit in the batch.");
                            }
                        }

                        try
                        {
                            // Use the producer client to send the batch of messages to the Service Bus queue


                            await topic_sender.SendMessagesAsync(topic_messageBatch);
                            Console.WriteLine($"A batch of {NumofMessages} messages has been published to the topic.");
                        }
                        finally
                        {
                            // Calling DisposeAsync on client types is required to ensure that network
                            // resources and other unmanaged objects are properly cleaned up.


                            await topic_sender.DisposeAsync();
                            await topic_client.DisposeAsync();
                        }
                    }
                    break;
                default:
                    telemetryClient.TrackTrace("No default for type");
                    break;

            }
  
             string responseMessage = string.IsNullOrEmpty(name)
                 ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                 : $"Hello, {name}. This HTTP triggered function executed successfully.";

             return new OkObjectResult(responseMessage);
        }
    }
}
