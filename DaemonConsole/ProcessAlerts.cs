using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using daemon_console.Models;

namespace daemon_console
{
    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    internal class ProcessAlerts
    {
        protected HttpClient HttpClient { get; private set; }

        //private readonly IElasticDbService _elasticDbService;

        private readonly ConfigModel _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient used to call the protected API</param>
        /// <param name="elasticDbService">elastic Db Service</param>
        /// <param name="config">elastic Index to write to, URL of the web API to call (supposed to return Json)</param>
        public ProcessAlerts(HttpClient httpClient, ConfigModel config)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            //_elasticDbService = elasticDbService ?? throw new ArgumentNullException(nameof(elasticDbService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calls the protected web API and processes the result
        /// </summary>
        public async Task Run()
        {
            var webApiUrl = $"{_config.ApiUrl}{_config.Alerts.Endpoint}";
            var response = await HttpClient.GetAsync(webApiUrl);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Successfully called the alerts web API");

                var stringResponse = await response.Content.ReadAsStringAsync();

                var jsonObject = JObject.Parse(stringResponse);

                //the part of the response we want is in "value" which is an array of json data
                var records = (JArray)jsonObject["value"];

                if (records.Any())
                {
                    foreach (var record in records)
                    {
                        var id = record.Value<string>("id");                       
                        
                        //var asyncIndexResponse = await _elasticDbService.IndexAsync(_config.Alerts.IndexName, id, record.ToString());

                        //new alerts into elastic
                        //if (asyncIndexResponse != null)
                        //{
                        //    if (!asyncIndexResponse.Success)
                        //    {
                        //        log.Error(asyncIndexResponse.Body);
                        //    }
                        //    else
                        //    {
                        //        log.Information("written alerts to elastic");
                        //    }                            
                        //}                                               
                    }
                }
                else
                {
                    Console.WriteLine("No new alerts");
                }                
            }
            else
            {
                Console.WriteLine($"Failed to call the alerts web API: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();

                // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                // this is because the tenant admin as not granted consent for the application to call the Web API
                Console.WriteLine($"Content: {content}");
            }
        }
    }
}
