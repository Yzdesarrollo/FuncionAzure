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
    public static class CreateBeaconFunction
    {
        private static CosmosClient cosmosClient;
        private static Database database;
        private static Container container;
        private static string databaseId = "BeaconsDatabase";
        private static string containerId = "BeaconsContainer";
        private static String endpointUri = "https://azcosmosdbbeacon.documents.azure.com:443";
        //private static String endpointUri = "https://localhost:8081";
        private static String primaryKey = "YUnfxhzkMgNPpb1btIMNxQJ15Z1ff6hilyTpnhx14u2OwRXdcxRFGWAp2Ew6Xtev7BkueRyGM8KNUB36mUnioQ==";
        //private static String primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        [FunctionName("CreateBeaconFunction")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

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

             string response = "{\"UUID\": \""+ data?.uuid  +"\", \"MAC\": \""+ data?.mac +"\", \"MAJOR\": \""+ data?.major  +"\", \"MINOR\": \""+ data?.minor +"\", \"MESSAGE\": \""+ data?.message +"\",\"status\":200, \"message\":\"OK\"}";

            string responseMessage;
            
            if (data?.mac == null){
                responseMessage = "{ \"status\":400, \"message\":\"Error HTTP 400 (Bad Request)\"}";
                log.LogError(responseMessage);
            }
            else{
                responseMessage = response;
                log.LogInformation(response);
            }

            return new OkObjectResult(CreateItemBeacons);
        }
    }
}
