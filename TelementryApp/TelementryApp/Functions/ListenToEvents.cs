using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TelementryApp.Helpers;
using TelementryApp.Models;

namespace TelementryApp.Functions
{
    public class ListenToEvents
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly IAzureStorageHelpers _azureStorageHelpers;

        public ListenToEvents(
            IConfiguration config,
            ILogger<ListenToEvents> logger,
            IAzureStorageHelpers azureStorageHelpers)
        {
            _config = config;
            _logger = logger;
            _azureStorageHelpers = azureStorageHelpers;
        }

        [FunctionName(nameof(ListenToEvents))]
        public async Task Run([CosmosDBTrigger(
            databaseName: "TelementryDB",
            collectionName: "DeviceReading",
            ConnectionStringSetting = "COSMOS_CONNECTION_STRING",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> input)
        {
            try
            {

                List<DeviceReading> backupDocuments = new List<DeviceReading>();
                CloudBlobClient cloudBlobClient = _azureStorageHelpers.ConnectToBlobClient(
                    _config[Constants.STORAGE_ACCOUNT_NAME],
                    _config[Constants.STORAGE_ACCOUNT_KEY]);
                CloudBlobContainer blobContainer = _azureStorageHelpers.GetBlobContainer(
                    cloudBlobClient, 
                    _config[Constants.STORAGE_CONTAINER]);
                string backupFile = Path.Combine("backup.json");

                if (input != null && input.Count > 0)
                {
                    foreach (var document in input)
                    {
                        // Persist to blob storage
                        var deviceReading = JsonConvert.DeserializeObject<DeviceReading>(document.ToString());
                        backupDocuments.Add(deviceReading);
                        _logger.LogInformation($"{document.Id} has been added to list");
                    }
                }

                var jsonData = JsonConvert.SerializeObject(backupDocuments);

                using (StreamWriter file = File.CreateText(backupFile))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, jsonData);                   
                }

                await _azureStorageHelpers.UploadBlobToStorage(blobContainer, backupFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Something went wrong. Exception thrown: {ex.Message}");
                throw;
            }
        }
    }
}
