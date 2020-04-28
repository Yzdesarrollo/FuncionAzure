/* using System;
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
// -----------------INSTANCIANDO-----------------------------------------//
        private static CosmosClient cosmosClient;
        private static Database database;
        private static Container container;
        private static String endpointUri = "https://localhost:8081";
        private static String primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private static string databaseId = "BeaconsDatabase";
        
        private static string containerId = "BeaconsContainer";
//-------------------------------------FUNCION----------------------------------------//
        [FunctionName("fn_beacons")]
        public static async Task<IActionResult> Run(
            
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("==================================================================================================================================");
            log.LogInformation("C# HTTP trigger function processed a request.");

           
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
      
//--------------------------INSTANCIANDO COSMOS CLIENT--------------------------------//
            cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions() { ApplicationName = "BeaconsACN" });

//-----------------------------CREANDO LA BASE DE DATOS---------------------------------//
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            log.LogInformation("Created Database: {0}\n", database.Id);
//-----------------------------CREANDO EL CONTENEDOR---------------------------//          
            container = await database.CreateContainerIfNotExistsAsync(containerId, "/mac", 400);
            log.LogInformation("Created Container: {0}\n", container.Id);
//----------------------------INSTANCIANDO BEACONS Y CREANDO EL ITEM-----------// 
            Beacons CreateItemBeacons = new Beacons
            {
                uuid = data?.uuid ?? null,
                mac = data?.mac ?? null,
                major = data?.major ?? null,
                minor = data?.minor ?? null,
                message = data?.message ?? null
            };

            try
            {
                //  Lea el item para ver si existe. 
                ItemResponse<Beacons> CreateItemBeaconsResponse = await container.ReadItemAsync<Beacons>(CreateItemBeacons.mac, new PartitionKey(CreateItemBeacons.mac));
                log.LogInformation(">>>");
                log.LogInformation("Item in database with id: {0} already exists\n", CreateItemBeaconsResponse.Resource.mac);
               
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Crea un item en el contenedor 
                ItemResponse<Beacons> CreateItemBeaconsResponse = await container.UpsertItemAsync<Beacons>(CreateItemBeacons, new PartitionKey(CreateItemBeacons.mac));
                log.LogInformation(">>>");
                log.LogInformation("Created item in database with id: {0} Operation consumed {1} RUs.\n", CreateItemBeaconsResponse.Resource.mac, CreateItemBeaconsResponse.RequestCharge);
                
            }
//----------------------------CONSULTANDO LA BASE DE DATOS-------------------------------//
            var sqlQueryText = "SELECT * FROM c";

            log.LogInformation(">>>");
            log.LogInformation("Running query: {0}\n", sqlQueryText);
            

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Beacons> queryResultSetIterator = container.GetItemQueryIterator<Beacons>(queryDefinition);

            List<Beacons> beaconQ = new List<Beacons>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Beacons> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Beacons beacon_x in currentResultSet)
                {
                    beaconQ.Add(beacon_x);
                    log.LogInformation(">>>");
                    log.LogInformation("\tRead {0}\n", beacon_x);
                    
                }
            }

            
            string response = "{\"address\": \""+ data?.address +"\", \"class\": \""+ data?.klass +"\", \"mac\": \""+ data?.mac +"\", \"name\": \""+ data?.name +"\", \"status\":200, \"message\":\"OK\"}";

            string responseMessage;
            
            if (data?.mac == null){
                responseMessage = "{ \"status\":400, \"message\":\"Error HTTP 400 (Bad Request)\"}";
                log.LogError(responseMessage);
            }
            else{
                responseMessage = response;
                log.LogInformation(response);
            }
                
            return new OkObjectResult(beaconQ);
        }
    }

} */
