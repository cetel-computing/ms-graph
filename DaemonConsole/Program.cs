using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates; //Only import this if you are using certificate
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using daemon_console.Models;

namespace daemon_console
{
    class Program
    {
        //private static IElasticDbService _elasticDbService;

        static void Main(string[] args)
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task RunAsync()
        {
            var config = ConfigModel.ReadFromJsonFile("appsettings.json");

            //_elasticDbService = new ElasticDbService(config.ElasticUri);

            foreach (var tenant in config.Tenants)
            {
                // You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
                var isUsingClientSecret = AppUsesClientSecret(tenant);

                // Even if this is a console application here, a daemon application is a confidential client application
                IConfidentialClientApplication app;

                if (isUsingClientSecret)
                {
                    app = ConfidentialClientApplicationBuilder.Create(tenant.ClientId)
                        .WithClientSecret(tenant.ClientSecret)
                        .WithAuthority(new Uri(tenant.Authority))
                        .Build();
                }
                else
                {
                    var certificate = ReadCertificate(tenant.CertificateName);
                    app = ConfidentialClientApplicationBuilder.Create(tenant.ClientId)
                        .WithCertificate(certificate)
                        .WithAuthority(new Uri(tenant.Authority))
                        .Build();
                }

                // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
                // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
                // a tenant administrator. 
                var scopes = new string[] { $"{config.ApiUrl}.default" };

                AuthenticationResult result = null;
                try
                {
                    result = await app.AcquireTokenForClient(scopes)
                        .ExecuteAsync();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Token acquired");
                    Console.ResetColor();
                }
                catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
                {
                    // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                    // Mitigation: change the scope to be as expected
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Scope provided is not supported");
                    Console.ResetColor();
                }

                if (result != null)
                {
                    var httpClient = new HttpClient();

                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        var defaultRequestHeaders = httpClient.DefaultRequestHeaders;
                        if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                        {
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        }

                        defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }

                    var apiCaller = new ProcessAlerts(httpClient, config);

                    await apiCaller.Run();

                    var apiCallerAuditLogs = new ProcessAuditLogs(httpClient, config, tenant);

                    await apiCallerAuditLogs.Run();

                    var apiCallerChats = new ProcessChats(httpClient, config);

                    await apiCallerChats.Run();
                }
            }
        }

        /// <summary>
        /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        /// </summary>
        /// <param name="config">Configuration from appsettings.json</param>
        /// <returns></returns>
        private static bool AppUsesClientSecret(GraphTenant config)
        {
            var clientSecretPlaceholderValue = "[Enter here a client secret for your application]";
            var certificatePlaceholderValue = "[Or instead of client secret: Enter here the name of a certificate (from the user cert store) as registered with your application]";

            if (!string.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
            {
                return true;
            }

            else if (!string.IsNullOrWhiteSpace(config.CertificateName) && config.CertificateName != certificatePlaceholderValue)
            {
                return false;
            }

            else
                throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        }

        private static X509Certificate2 ReadCertificate(string certificateName)
        {
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            var certificateDescription = CertificateDescription.FromStoreWithDistinguishedName(certificateName);
            var defaultCertificateLoader = new DefaultCertificateLoader();

            defaultCertificateLoader.LoadIfNeeded(certificateDescription);

            return certificateDescription.Certificate;
        }
    }
}