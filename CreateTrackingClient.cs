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
    public static class CreateTrackingClient
    {
        private static CosmosClient cosmosClient;
        private static Database database;
        private static Container container;
        private static string databaseId = "BeaconsDatabase";
        private static string containerId = "TrackingContainer";
        private static String endpointUri = "https://azcosmosdbbeacons.documents.azure.com:443/";
        //private static String endpointUri = "https://localhost:8081";
        // private static String primaryKey = "YUnfxhzkMgNPpb1btIMNxQJ15Z1ff6hilyTpnhx14u2OwRXdcxRFGWAp2Ew6Xtev7BkueRyGM8KNUB36mUnioQ==";
        private static String primaryKey = "h9PEdi1ArxmhAVo9tjvsB8z0ezbMYHGoami8RF4p5AWnySRdkQ3jtbIAudsHIRZ6wg4UXAwdCcGxLOKlPHQpbA==";

        [FunctionName("CreateTrackingClient")]
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
            
            Users CreateItemTraking = new Users
            {
                id = data?.id ?? null,
                mac = data?.mac ?? null,
                day = data?.day ?? null,
                hour = data?.hour ?? null,
                user = data?.user ?? null
            };
              
   try
    {
        //  Lea el item para ver si existe. 
        ItemResponse<Users> CreateItemTrackingResponse = await container.ReadItemAsync<Users>(CreateItemTraking.mac, new PartitionKey(CreateItemTraking.mac));
        log.LogInformation(">>>");
        log.LogInformation("Item in database with id: {0} already exists\n", CreateItemTrackingResponse.Resource.mac);
        
    }
    catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        // Crea un item en el contenedor 
        ItemResponse<Users> CreateItemTrackingResponse = await container.UpsertItemAsync<Users>(CreateItemTraking, new PartitionKey(CreateItemTraking.mac));
        log.LogInformation(">>>");
        log.LogInformation("Created item in database with id: {0} Operation consumed {1} RUs.\n", CreateItemTrackingResponse.Resource.mac, CreateItemTrackingResponse.RequestCharge);
        
    }
        string response = "{\"ID\": \""+ data?.id  +"\", \"MAC\": \""+ data?.mac +"\", \"DAY\": \""+ data?.day  +"\", \"HOUR\": \""+ data?.hour+"\", \"USER\": \""+ data?.user +"\",\"status\":200, \"message\":\"OK\"}";

        string responseMessage;
        
        if (data?.mac == null){
            responseMessage = "{ \"status\":400, \"message\":\"Error HTTP 400 (Bad Request)\"}";
            log.LogError(responseMessage);
        }
        else{
            responseMessage = response;
            log.LogInformation(response);
        }

        return new OkObjectResult(CreateItemTraking);
        }
    }
}
