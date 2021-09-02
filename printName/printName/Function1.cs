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
using Azure.Messaging.ServiceBus;

namespace printName
{
    public class printName
    {
        private const string V = "printName";
        private TelemetryClient telemetryClient;

        private string connection_string = "Endpoint=sb://joelservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=BKpk29NbbFOC27yxFOfdfPJtjYqkZNuS+BijOl2+JOQ=";
        
        private string queue_name = "demoqueue";
        private string topicName = "demotopic";
        private ServiceBusClient queue_client;
        private ServiceBusClient topic_client;

        private ServiceBusSender queue_sender;
        private ServiceBusSender topic_sender;

        private int numOfMessages;

        /// Using dependency injection will guarantee that you use the same configuration for telemetry collected automatically and manually.
        public printName(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName(V)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request by Joel.");

            string name = req.Query["name"];
            string type = req.Query["type"];
            string count = req.Query["count"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            type = type ?? data.type;
            count = count ?? data.count;

            numOfMessages = Int32.Parse(count);


            if (name.Equals("joel"))
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("nameParameter", name);
                telemetryClient.TrackEvent("Joel is passed", dictionary);
                telemetryClient.TrackTrace("Joel has been passed as arguement");
            }
            else
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("nameParameter", name);
                telemetryClient.TrackEvent("Other Name is passed", dictionary);
                telemetryClient.TrackTrace("Some other name is passed as argument");
            }

            switch(type)
            {
                case "queue":
                    {
                        queue_client = new ServiceBusClient(connection_string);
                        queue_sender = queue_client.CreateSender(queue_name);

                        using ServiceBusMessageBatch queue_messageBatch = await queue_sender.CreateMessageBatchAsync();

                        for(int i=1;i<=numOfMessages;i++)
                        {
                            if (!queue_messageBatch.TryAddMessage(new ServiceBusMessage(name+$" {i}")))
                            {
                                // if it is too large for the batch
                                throw new Exception($"The message "+ name+" {i} is too large to fit in the batch.");
                            }
                        }

                        try
                        {
                            // Use the producer client to send the batch of messages to the Service Bus queue
                            await queue_sender.SendMessagesAsync(queue_messageBatch);
                            Console.WriteLine($"A batch of {numOfMessages} messages has been published to the queue.");
                            telemetryClient.TrackTrace($"A batch of {numOfMessages} messages has been published to the queue.");
                        }
                        finally
                        {
                            // Calling DisposeAsync on client types is required to ensure that network
                            // resources and other unmanaged objects are properly cleaned up.
                            await queue_sender.DisposeAsync();
                            await queue_client.DisposeAsync();
                        }
                    }
                    break;
                case "topic":
                    {
                        topic_client = new ServiceBusClient(connection_string);
                        topic_sender = topic_client.CreateSender(topicName);

                        using ServiceBusMessageBatch topic_messageBatch = await topic_sender.CreateMessageBatchAsync();


                        for (int i = 1; i <= numOfMessages; i++)
                        {
                            // try adding a message to the batch
                            if (!topic_messageBatch.TryAddMessage(new ServiceBusMessage(name+$" {i}")))
                            {
                                // if it is too large for the batch
                                throw new Exception("The message "+name+ $" {i} is too large to fit in the batch.");
                            }
                        }

                        try
                        {
                            // Use the producer client to send the batch of messages to the Service Bus topic
                            await topic_sender.SendMessagesAsync(topic_messageBatch);
                            Console.WriteLine($"A batch of {numOfMessages} messages has been published to the topic.");
                            telemetryClient.TrackTrace($"A batch of {numOfMessages} messages has been published to the topic");
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
                    break;
            }

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
