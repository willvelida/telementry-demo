using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TelementryApp.Helpers;

[assembly: WebJobsStartup(typeof(Startup))]
namespace TelementryApp.Helpers
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });

            var config = (IConfiguration)builder.Services.First(d => d.ServiceType == typeof(IConfiguration)).ImplementationInstance;

            builder.Services.AddSingleton(s => new CosmosClient(config[Constants.COSMOS_CONNECTION_STRING]));
            builder.Services.AddSingleton(
                s => new EventHubProducerClient(config[Constants.EVENT_HUB_CONNECTION_STRING], config[Constants.EVENT_HUB_NAME]));

            builder.Services.AddSingleton<IAzureStorageHelpers, AzureStorageHelpers>();
        }
    }
}
