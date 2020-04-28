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
    public static class QueryBeaconFunction
    {
        private static CosmosClient cosmosClient;
        private static Database database;
        private static Container container;
        private static String databaseId = "BeaconsDatabase";
        private static String containerId = "BeaconsContainer";
        private static String endpointUri = "https://azcosmosdbbeacon.documents.azure.com:443";
        //private static String endpointUri = "https://localhost:8081";
        private static String primaryKey = "YUnfxhzkMgNPpb1btIMNxQJ15Z1ff6hilyTpnhx14u2OwRXdcxRFGWAp2Ew6Xtev7BkueRyGM8KNUB36mUnioQ==";
        //private static String primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        [FunctionName("QueryBeaconFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            List<Beacons> requestBeaconList = new List<Beacons>();

            foreach (dynamic item in data)
            {
                Beacons requestBeaconItem = new Beacons
                {
                    uuid = item?.uuid ?? null,
                    mac = item?.mac ?? null,
                    major = item?.major ?? null,
                    minor = item?.minor ?? null,
                    message = item?.message ?? null
                };

                requestBeaconList.Add(requestBeaconItem);
            }

            cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions() { ApplicationName = "BeaconsACN" });

            database = cosmosClient.GetDatabase(databaseId);
            
            container = database.GetContainer(containerId);
            
            var sqlQueryText = "SELECT * FROM c";

            log.LogInformation(">>>");
            log.LogInformation("Running query: {0}\n", sqlQueryText);
            
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            FeedIterator<Beacons> queryResultSetIterator = container.GetItemQueryIterator<Beacons>(queryDefinition);

            List<Beacons> beaconQ = new List<Beacons>();
            log.LogInformation(Convert.ToString(queryResultSetIterator));
            log.LogInformation(Convert.ToString(queryResultSetIterator.HasMoreResults));
            while (queryResultSetIterator.HasMoreResults)
            { 
                FeedResponse<Beacons> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                log.LogInformation(Convert.ToString(currentResultSet));
                foreach (Beacons beacon_x in currentResultSet)
                {  
                    foreach(Beacons beacon_item in requestBeaconList){

                        if(beacon_item.mac == beacon_x.mac){
                            beaconQ.Add(beacon_x);
                            log.LogInformation(">>>");
                            log.LogInformation("\tRead {0}\n", beacon_x);
                        }
                    }
                }
            log.LogInformation(Convert.ToString(queryResultSetIterator.HasMoreResults));
            }

            // if(beaconQ == null){
            //     Beacons voidBeacon = new Beacons
            //     {
            //         mensaje = "No se encontro registro"
            //     }; 
            //     beaconQ.Add(voidBeacon);
            // }
     
            return new OkObjectResult(beaconQ);
        }
    }
}
