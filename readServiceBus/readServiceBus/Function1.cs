using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace readServiceBus
{
    public class Function1
    {
        private TelemetryClient telemetryClient;
        static ServiceBusClient queue_client;

        static ServiceBusClient topic_client;

        static string connectionString =
            "Endpoint=sb://joelservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=BKpk29NbbFOC27yxFOfdfPJtjYqkZNuS+BijOl2+JOQ=";

        static string topicConnectionString =
            "Endpoint=sb://joelservicebus.servicebus.windows.net/;SharedAccessKeyName=listen-topic-key;SharedAccessKey=uKUwK+KwPO0q9XX28BI/s6TzJfj5oAGoUvs5+U210Tk=;EntityPath=demotopic";
        
        static string queueName = "demoqueue";

        static string topicName = "demotopic";

        static string subscriptionName = "demoSubscription";

        static ServiceBusSender sender;
        
        static ServiceBusSender topic_sender;

        public Function1(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = TelemetryFactory.GetTelemetryClient();
        }

        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a read queue request");

            string type = req.Query["type"];
            string noOfMessages = req.Query["count"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            type = type ?? data?.type;
            noOfMessages = noOfMessages ?? data.count;

            queue_client = new ServiceBusClient(connectionString);

            topic_client = new ServiceBusClient(topicConnectionString);

            telemetryClient.TrackTrace("He");



            switch (type)
            {
                case "queue":
                    {

                        ServiceBusReceiver receiver = queue_client.CreateReceiver(
                            queueName, new ServiceBusReceiverOptions()
                            {
                                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                            });

                        var messages_received = receiver.ReceiveMessagesAsync(Int32.Parse(noOfMessages)).GetAwaiter().GetResult();
                        log.LogInformation("Queue Messages Received");

                        foreach (var message in messages_received)
                        {
                            log.LogInformation(message.SequenceNumber.ToString());
                            log.LogInformation(message.Body.ToString());
                            telemetryClient.TrackTrace($"Receiving Messages from Queue {queueName}");
                        }

                        await queue_client.DisposeAsync();
                        await receiver.DisposeAsync();

                    }
                    break;
                case "topic":
                    {

                        ServiceBusReceiver topic_receiver = topic_client.CreateReceiver(
                            topicName, subscriptionName, new ServiceBusReceiverOptions()
                            {
                                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                            });

                        var messages_received = topic_receiver.ReceiveMessagesAsync(Int32.Parse(noOfMessages)).GetAwaiter().GetResult();
                        log.LogInformation("Topic Messages Received");

                        foreach(var message in messages_received)
                        {
                            log.LogInformation(message.SequenceNumber.ToString());
                            log.LogInformation(message.Body.ToString());
                            telemetryClient.TrackTrace($"Receiving messages from topic {topicName}");
                        }
                        await topic_client.DisposeAsync();
                        await topic_receiver.DisposeAsync();
                    }
                    break;
                default:
                    break;
            }

            string responseMessage = "All Messages were read sucessfully";

            return new OkObjectResult(responseMessage);
        }

    }

    
}
