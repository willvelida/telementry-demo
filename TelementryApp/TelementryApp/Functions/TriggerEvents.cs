using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Bogus;
using TelementryApp.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using System.Text;

namespace TelementryApp.Functions
{
    public class TriggerEvents
    {
        private readonly ILogger<TriggerEvents> _logger;
        private readonly IConfiguration _config;
        private readonly EventHubProducerClient _eventHubProducerClient;

        public TriggerEvents(
            ILogger<TriggerEvents> logger,
            IConfiguration config,
            EventHubProducerClient eventHubProducerClient)
        {
            _logger = logger;
            _config = config;
            _eventHubProducerClient = eventHubProducerClient;
        }

        [FunctionName(nameof(TriggerEvents))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "TriggerEvents")] HttpRequest req)
        {
            IActionResult result = null;

            try
            {
                var deviceIterations = new Faker<DeviceReading>()
                .RuleFor(i => i.DeviceId, (fake) => Guid.NewGuid().ToString())
                .RuleFor(i => i.DeviceTemperature, (fake) => Math.Round(fake.Random.Decimal(0.00m, 30.00m), 2))
                .RuleFor(i => i.DamageLevel, (fake) => fake.PickRandom(new List<string> { "Low", "Medium", "High" }))
                .RuleFor(i => i.DeviceAgeInDays, (fake) => fake.Random.Number(1, 60))
                .GenerateLazy(5000);

                foreach (var reading in deviceIterations)
                {
                    EventDataBatch eventDataBatch = await _eventHubProducerClient.CreateBatchAsync();
                    var eventReading = JsonConvert.SerializeObject(reading);
                    eventDataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(eventReading)));
                    await _eventHubProducerClient.SendAsync(eventDataBatch);
                    _logger.LogInformation($"Sending {reading.DeviceId} to event hub");
                }
              
                result = new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Something went wrong. Exception thrown: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
