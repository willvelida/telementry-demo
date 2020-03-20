using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TelementryApp.Helpers;
using TelementryApp.Models;

namespace TelementryApp.Functions
{
    public class PersistEvents
    {
        private readonly ILogger<PersistEvents> _logger;
        private readonly IConfiguration _config;
        private CosmosClient _cosmosClient;
        private Container _telementryContainer;

        public PersistEvents(
            ILogger<PersistEvents> logger,
            IConfiguration config,
            CosmosClient cosmosClient)
        {
            _logger = logger;
            _config = config;
            _cosmosClient = cosmosClient;
            _telementryContainer = _cosmosClient.GetContainer(_config[Constants.TELEMENTRY_DATABASE],_config[Constants.TELEMENTRY_CONTAINER]);
        }

        [FunctionName(nameof(PersistEvents))]
        public async Task Run([EventHubTrigger("telementryreadings",
            Connection = "EVENT_HUB_CONNECTION_STRING")] EventData[] events)
        {

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    var telementryEvent = JsonConvert.DeserializeObject<DeviceReading>(messageBody);

                    // Persist to cosmos db
                    await _telementryContainer.CreateItemAsync(telementryEvent);
                    _logger.LogInformation($"{telementryEvent.DeviceId} has been persisted");                  
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong. Exception thrown: {ex.Message}");
                }
            }
        }
    }
}
