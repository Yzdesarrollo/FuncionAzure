using System;
using System.IO;

using System.Net;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;



namespace Beacons.Function
{
    
    public static class fn_beacons
    {

        private static CosmosClient cosmosClient;
        private static Database database;
        private static Container container;
        private static String endpointUri = "https://localhost:8081";
        private static String primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        // private static string databaseId = "taskDatabase";
        // private static string collectionId = "TaskCollection";
        // private static string containerId = "Items";

        private static string databaseId = "BeaconsDatabase";
        //private static string collectionId = "collectionBeacons";
        private static string containerId = "BeaconsContainer";

        [FunctionName("fn_beacons")]
        public static async Task<IActionResult> Run(
            //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("==================================================================================================================================");
            log.LogInformation("C# HTTP trigger function processed a request.");

            // GET
            //string mac = req.Query["mac"];

            // POST
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions() { ApplicationName = "BeaconsACN" });

            //var sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            log.LogInformation("Created Database: {0}\n", database.Id);
            container = await database.CreateContainerIfNotExistsAsync(containerId, "/mac", 400);
            log.LogInformation("Created Container: {0}\n", container.Id);

            Beacons CreateItemBeacons = new Beacons
            {
                address = data?.address,
                klass = data?.klass,
                mac = data?.mac,
                name = data?.name
            };

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Beacons> CreateItemBeaconsResponse = await container.ReadItemAsync<Beacons>(CreateItemBeacons.mac, new PartitionKey(CreateItemBeacons.mac));
                log.LogInformation(">>>");
                log.LogInformation("Item in database with id: {0} already exists\n", CreateItemBeaconsResponse.Resource.mac);
               
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container 
                ItemResponse<Beacons> CreateItemBeaconsResponse = await container.UpsertItemAsync<Beacons>(CreateItemBeacons, new PartitionKey(CreateItemBeacons.mac));
                log.LogInformation(">>>");
                log.LogInformation("Created item in database with id: {0} Operation consumed {1} RUs.\n", CreateItemBeaconsResponse.Resource.mac, CreateItemBeaconsResponse.RequestCharge);
                
            }

            var sqlQueryText = "SELECT * FROM c";

            log.LogInformation(">>>");
            log.LogInformation("Running query: {0}\n", sqlQueryText);
            //Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Beacons> queryResultSetIterator = container.GetItemQueryIterator<Beacons>(queryDefinition);

            List<Beacons> beaconsindb = new List<Beacons>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Beacons> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Beacons beacon_x in currentResultSet)
                {
                    beaconsindb.Add(beacon_x);
                    log.LogInformation(">>>");
                    log.LogInformation("\tRead {0}\n", beacon_x);
                    
                }
            }

           /*
            foreach (var item in collection)
            {
                
            }
             var client = new HttpClient();
            client.BaseAddress = new Uri("https://www.google.com");
            var result = await client.GetAsync("");
            string resultContent = await result.Content.ReadAsStringAsync(); */
            
            
            string response = "{\"address\": \"F8:F0:05:C0:74:09\", \"class\": 7936, \"id\": \""+ data?.mac +"\", \"name\": null, \"status\":200, \"message\":\"OK\"}";

            string responseMessage;
            
            if (data?.mac == null){
                responseMessage = "{ \"status\":400, \"message\":\"Error HTTP 400 (Bad Request)\"}";
            }
            else{
                responseMessage = response;
            }
                
            return new OkObjectResult(beaconsindb);
        }
    }

}
