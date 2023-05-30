// See https://aka.ms/new-console-template for more information

namespace ChangeFeed.DbInitialization
{
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Configuration;

    internal class Program
    {
        private static readonly string CosmosDatabaseId = "products";

        // Async main requires c# 7.1 which is set in the csproj with the LangVersion attribute
        // <Main>
        public static async Task Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appSettings.json")
                    .Build();

                string? endpoint = configuration["EndPointUrl"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new ArgumentNullException("Please specify a valid endpoint in the appSettings.json");
                }

                string? authKey = configuration["AuthorizationKey"];
                if (string.IsNullOrEmpty(authKey) || string.Equals(authKey, "Super secret key"))
                {
                    throw new ArgumentException("Please specify a valid AuthorizationKey in the appSettings.json");
                }

                //Read the Cosmos endpointUrl and authorizationKeys from configuration
                //These values are available from the Azure Management Portal on the Cosmos Account Blade under "Keys"
                //NB > Keep these values in a safe & secure location. Together they provide Administrative access to your Cosmos account
                using (CosmosClient cosmosClient = new CosmosClient(endpoint, authKey))
                {
                    await Program.InitializeDb(cosmosClient);
                }
            }
            catch (CosmosException cre)
            {
                Console.WriteLine(cre.ToString());
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }
        // </Main>

        public static async Task InitializeDb(CosmosClient cosmosClient)
        {
            // Create the database
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(Program.CosmosDatabaseId);
            Console.WriteLine($"Created database {databaseResponse.Database.Id}");

            // Create the lease container
            var leaseContainerResponse = await databaseResponse.Database.CreateContainerAsync(
                new ContainerProperties("lease", "/id"));
            Console.WriteLine($"Created lease container {leaseContainerResponse.Container.Id}");

            // Create the cart container partitioned on /cartId with TTL enabled
            var cartContainerResponse = await databaseResponse.Database.CreateContainerAsync(
                new ContainerProperties
                {
                    Id = "cart",
                    PartitionKeyPath = "/cartId",
                    DefaultTimeToLive = -1,
                });
            Console.WriteLine($"Created cart container {cartContainerResponse.Container.Id}");

            // Create the product container partitioned on /categoryId
            var productContainerResponse = await databaseResponse.Database.CreateContainerAsync(
                new ContainerProperties
                {
                    Id = "product",
                    PartitionKeyPath = "/categoryId",
                });
            Console.WriteLine($"Created product container {productContainerResponse.Container.Id}");

            // Create the productMeta container partitioned on /type
            var productMetaContainerResponse = await databaseResponse.Database.CreateContainerAsync(
                new ContainerProperties
                {
                    Id = "productMeta",
                    PartitionKeyPath = "/type",
                });
            Console.WriteLine($"Created productMeta container {productMetaContainerResponse.Container.Id}");
        }
    }
}
