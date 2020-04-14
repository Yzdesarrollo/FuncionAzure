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
    // private static HttpClient httpClient = new HttpClient();

    // // Create a single, static HttpClient
    // public static async Task Run(string input)
    // {
    //     var response = await httpClient.GetAsync("https://example.com");
    //     // Rest of function
    // }
    public static class fn_beacons
    {

        private static CosmosClient cosmosClient;
        private static Database database;
        private static Container container;
        private static String endpointUri = "https://azcosmosdbbeacon.documents.azure.com:443/";
        private static String primaryKey = "YUnfxhzkMgNPpb1btIMNxQJ15Z1ff6hilyTpnhx14u2OwRXdcxRFGWAp2Ew6Xtev7BkueRyGM8KNUB36mUnioQ==";

        // private static string databaseId = "taskDatabase";
        // private static string collectionId = "TaskCollection";
        // private static string containerId = "Items";

        private static string databaseId = "databaseBeacons";
        private static string collectionId = "collectionBeacons";
        private static string containerId = "containerBeacons";

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
            
            
            // GET is Null => Use the POST data
            //mac = mac ?? data?.mac;

            // ==================================================================================================================================
            // https://stackoverflow.com/questions/42482223/azure-functions-call-http-post-inside-function
            // https://github.com/Azure-Samples/cosmos-dotnet-getting-started
            // https://stackoverflow.com/questions/58121736/partitionkey-extracted-from-document-doesnt-match-the-one-specified-in-the-head
            // Aqui corresponde el codigo de la peticion a la Base de datos 
            // enviar request a la DB

            cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions() { ApplicationName = "BeaconsACN" });

            //var sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            //database = cosmosClient.GetDatabase("databaseid");
            container = await database.CreateContainerIfNotExistsAsync(containerId, "/mac", 400);
            //container = database.GetContainer("containerid");

            // int? throughput = await container.ReadThroughputAsync();
            // if (throughput.HasValue)
            // {
            //     Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
            //     int newThroughput = throughput.Value + 100;
            //     // Update throughput
            //     await container.ReplaceThroughputAsync(newThroughput);
            //     Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
            // }

            Beacon beaconX = new Beacon
            {
                address = data?.address,
                klass = data?.klass,
                mac = data?.mac,
                name = data?.name
            };


            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Beacon> beaconXResponse = await container.ReadItemAsync<Beacon>(beaconX.mac, new PartitionKey(beaconX.mac));
                log.LogInformation(">>>");
                log.LogInformation("Item in database with id: {0} already exists\n", beaconXResponse.Resource.mac);
                //Console.WriteLine("Item in database with id: {0} already exists\n", beaconXResponse.Resource.mac);
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                ItemResponse<Beacon> beaconXResponse = await container.CreateItemAsync<Beacon>(beaconX, new PartitionKey(beaconX.mac));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                log.LogInformation(">>>");
                log.LogInformation("Created item in database with id: {0} Operation consumed {1} RUs.\n", beaconXResponse.Resource.mac, beaconXResponse.RequestCharge);
                //Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", beaconXResponse.Resource.mac, beaconXResponse.RequestCharge);
            }

            var sqlQueryText = "SELECT * FROM c";

            log.LogInformation(">>>");
            log.LogInformation("Running query: {0}\n", sqlQueryText);
            //Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Beacon> queryResultSetIterator = container.GetItemQueryIterator<Beacon>(queryDefinition);

            List<Beacon> beaconsindb = new List<Beacon>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Beacon> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Beacon beacon_x in currentResultSet)
                {
                    beaconsindb.Add(beacon_x);
                    log.LogInformation(">>>");
                    log.LogInformation("\tRead {0}\n", beacon_x);
                    //Console.WriteLine("\tRead {0}\n", beacon_x);
                }
            }



            // using(var client = new HttpClient())
            // {
            //     client.BaseAddress = new Uri("https://www.google.com");
            //     var result = await client.GetAsync("");
            //     string resultContent = await result.Content.ReadAsStringAsync();
            //     log.LogInformation(resultContent);
            // }

            //https://stackoverflow.com/questions/42482223/azure-functions-call-http-post-inside-function   

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://www.google.com");
            var result = await client.GetAsync("");
            string resultContent = await result.Content.ReadAsStringAsync();
            //log.LogInformation(resultContent);
            

            // HttpRequest sqlreq;

            // sqlreq.Body = "{}";          

            // string response = en esta variable almacenamos la respuesta de la BD

            // ==================================================================================================================================
            
            string response = "{\"address\": \"F8:F0:05:C0:74:09\", \"class\": 7936, \"id\": \""+ data?.mac +"\", \"name\": null, \"status\":200, \"message\":\"OK\"}";

            // string responseMessage = string.IsNullOrEmpty(name)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //     : $"Hello, {name}. This HTTP triggered function executed successfully.";

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
