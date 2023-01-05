using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using System.Reflection;
using System.Text.Json;
using System.IO;
namespace Modul3.Pages
{
    public class IndexModel : PageModel
    {

        private const string blobServiceEndpoint = "https://myimagestorages8372dm.blob.core.windows.net/";
        private const string storageAccountName = "myimagestorages8372dm";
        private const string storageAccountKey = "+9nUCSJEVk+dGVb6z3VPdcJZDQXbWlzyRPBelLbb9VmIYcVuKnEZm8lLZkjVC4W3H659MkpJNuKm+AStFil/Vw==";
        private const string containerName = "obrazkidoaplikacji";



        private const string EndpointUrl = "https://modul4data.documents.azure.com:443/";
        private const string AuthorizationKey = "oCTkQo9UKKpP2axAYbofkeVKvUXl8Wfsof3ivXi34B09Q8n9BTUVde29rGSWepGmAiiU93WRMn8TACDbNMcvqA==";
        private const string DatabaseName = "Modul4DB";
        private const string ContainerName = "Obrazy";
        private const string PartitonKey = "/Tag";
        private const string JsonFilePath = "C:/Users/Programowanko/Desktop/mymodels.json";
        static private int amountToInsert;
        static List<ImageDescription> models;


        public List<BlobClient> blobClients = new List<BlobClient>();

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet(string tag)
        {
            StorageSharedKeyCredential accountCredentials = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);
            BlobServiceClient serviceClient = new BlobServiceClient(new Uri(blobServiceEndpoint), accountCredentials);
            AccountInfo info = await serviceClient.GetAccountInfoAsync();
            BlobContainerClient container = serviceClient.GetBlobContainerClient(containerName);

            var obrazy = await PobierzWpisyCosmoDb();

            if (!String.IsNullOrEmpty(tag))
            {
                obrazy = obrazy.Where( x => x.Tag.Equals(tag)).ToList();
            }

            foreach (ImageDescription blob in obrazy)
            {
 
                blobClients.Add(container.GetBlobClient(blob.ImageName));
            }


            foreach (var a in blobClients)
            {
                Debug.WriteLine(a.Uri);
            }
        }

        public async Task OnPost()
        {
            StorageSharedKeyCredential accountCredentials = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);
            BlobServiceClient serviceClient = new BlobServiceClient(new Uri(blobServiceEndpoint), accountCredentials);
            AccountInfo info = await serviceClient.GetAccountInfoAsync();
            BlobContainerClient container = serviceClient.GetBlobContainerClient(containerName);

            

            if(Request.Form.Files.Count == 0)
            {
                container.DeleteBlob(Request.Form["blobName"]);
            }
            else
            {
                foreach(var file in Request.Form.Files)
                {
                    ImageDescription img = new ImageDescription
                    {
                        Name = Request.Form["name"],
                        Description = Request.Form["description"],
                        Tag = Request.Form["tag"],
                        id = Guid.NewGuid().ToString(),
                        ImageName = file.FileName
                    };
                    await DodajWpisDoCosmoDB(img);
                    await container.UploadBlobAsync(file.FileName, file.OpenReadStream());
                }
                
            }
           
        }

        public async Task DodajWpisDoCosmoDB(ImageDescription imageDescription)
        {
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
            Container container = database.GetContainer(ContainerName);

            imageDescription.id = Guid.NewGuid().ToString();
                var result = await container.CreateItemAsync(imageDescription, new PartitionKey(imageDescription.Tag));
        }

        public async Task<List<ImageDescription>> PobierzWpisyCosmoDb()
        {
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
            Container container = database.GetContainer(ContainerName);

            string query = $@"SELECT * FROM items";
            var iterator = container.GetItemQueryIterator<ImageDescription>(query);
            List<ImageDescription> matches = new List<ImageDescription>();
            while (iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                matches.AddRange(next);
            }
            return matches;
        }



    }

    public class ImageDescription
    {
        public string id { get; set; }
        public string Name { get; set; }

        public string Tag { get; set; }
        public string Description { get; set; }
        public DateTime DateOfCreate { get; set; }
        public string ImageName { get; set; }
    }
}