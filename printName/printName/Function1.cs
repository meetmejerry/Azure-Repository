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

namespace printName
{
    public class printName
    {
        private const string V = "printName";
        private TelemetryClient telemetryClient;


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
            

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

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


            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
