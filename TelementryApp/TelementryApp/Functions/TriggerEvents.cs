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

namespace TelementryApp.Functions
{
    public class TriggerEvents
    {
        private readonly ILogger<TriggerEvents> _logger;

        public TriggerEvents(
            ILogger<TriggerEvents> logger)
        {
            _logger = logger;
        }

        [FunctionName(nameof(TriggerEvents))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "TriggerEvents")] HttpRequest req,
            [EventHub("telementryreadings",
            Connection = "EVENT_HUB_CONNECTION_STRING")] IAsyncCollector<DeviceReading> outputEvents)
        {
            IActionResult result = null;

            try
            {
                var deviceIterations = new Faker<DeviceReading>()
                .RuleFor(i => i.DeviceId, (fake) => Guid.NewGuid().ToString())
                .RuleFor(i => i.Temperature, (fake) => fake.Random.Decimal(0.00m, 30.00m))
                .RuleFor(i => i.Level, (fake) => fake.PickRandom(new List<string> { "Low", "Medium", "High" }))
                .RuleFor(i => i.DeviceAgeInDays, (fake) => fake.Random.Number(1, 60))
                .GenerateLazy(5000);

                foreach (var reading in deviceIterations)
                {
                    await outputEvents.AddAsync(reading);                   
                }

                await outputEvents.FlushAsync();

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
