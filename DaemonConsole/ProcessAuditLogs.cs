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
    internal class ProcessAuditLogs
    {
        protected HttpClient HttpClient { get; private set; }

        //private readonly IElasticDbService _elasticDbService;

        private readonly ConfigModel _config;

        //private EmailSender _smtp;

        //private TokenHandler _tokenHandler;

        private string _path;

        private string _configFile;

        private string _emailLogo;

        private readonly GraphTenant _tenant;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient used to call the protected API</param>
        /// <param name="elasticDbService">elastic Db Service</param>
        /// <param name="config">app config</param>
        /// <param name="tenant">tenant config</param>
        public ProcessAuditLogs(HttpClient httpClient, ConfigModel config, GraphTenant tenant)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            //_elasticDbService = elasticDbService ?? throw new ArgumentNullException(nameof(elasticDbService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Calls the protected web API and processes the result
        /// </summary>
        public async Task Run()
        {
            var webApiUrl = $"{_config.ApiUrl}{_config.AuditLogs.Endpoint}";
            var response = await HttpClient.GetAsync(webApiUrl);
            
            //assume the html is in the same location as config
            //_configFile = Path.GetFileName(_path);
            //_emailLogo = _path.Replace(_configFile, _config.EmailLogo);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Successfully called the audit logs web API");

                var stringResponse = await response.Content.ReadAsStringAsync();

                var jsonObject = JObject.Parse(stringResponse);

                //the part of the response we want is in "value" which is an array of json data
                var records = (JArray)jsonObject["value"];

                if (records.Any())
                {
                    foreach (var record in records)
                    {
                        var id = record.Value<string>("id");

                        //add the tenantId
                        record["tenantId"] = _tenant.Tenant;

                        //var asyncIndexResponse = await _elasticDbService.IndexAsync(_config.AuditLogs.IndexName, id, record.ToString());

                        //new alerts into elastic
                        //if (asyncIndexResponse != null)
                        //{                            
                        //    if (!asyncIndexResponse.Success)
                        //    {
                        //        log.Error(asyncIndexResponse.Body);
                        //    }
                        //    else
                        //    {
                        //        log.Information("written audit logs to elastic");

                        //        var activity = record.Value<string>("activityDisplayName");
                        //        var initiatedById = record["initiatedBy"]["user"]["id"].ToString();

                        //        if (_tenant.ActivitiesToEmail.Any() && _tenant.ActivitiesToEmail.Contains(activity) && initiatedById != Guid.Empty.ToString())
                        //        {
                        //            var target = record["targetResources"][0]["userPrincipalName"].ToString();

                        //            SendAlertEmail(log, record, activity, target);
                        //        }
                        //    }                                                       
                        //}                                               
                    }
                }
                else
                {
                    Console.WriteLine("No new audit logs");
                }
            }
            else
            {
                Console.WriteLine($"Failed to call the audit logs web API: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();

                // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                // this is because the tenant admin as not granted consent for the application to call the Web API
                Console.WriteLine($"Content: {content}");
            }
        }

        /// <summary>
        /// Email new alerts to admins
        /// </summary>
        //private void SendAlertEmail(ILogger log, JToken record, string activity, string target)
        //{
        //    log.Information("emailing alerts");
                        
        //    var emailTemplate = _path.Replace(_configFile, _config.AuditLogs.EmailTemplate);
        //    var emailBody = File.ReadAllText(emailTemplate);

        //    var initiatedBy = record["initiatedBy"]["user"]["userPrincipalName"].ToString();
        //    var activityDateTime = record.Value<string>("activityDateTime");
        //    var correlationId = record.Value<string>("correlationId");

        //    emailBody = emailBody.Replace("{activity}", activity)
        //                      .Replace("{target}", target)
        //                      .Replace("{initiatedBy}", initiatedBy)
        //                      .Replace("{activityDateTime}", activityDateTime)
        //                      .Replace("{correlationId}", correlationId);
        //    try
        //    {
        //        _smtp.SendMail(_config.EmailSettings, _tenant.SendTo, "MS GRAPH ALERT: " + activity, emailBody, _emailLogo);
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error(ex, "Exception in SendMail");
        //    }
        //}
    }    
}
