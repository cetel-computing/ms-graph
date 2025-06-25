using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using daemon_console.Models;

namespace daemon_console
{
    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    internal class ProcessChats
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
        public ProcessChats(HttpClient httpClient, ConfigModel config)
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
            var nextLink = $"{_config.ApiUrl}{_config.Users.Endpoint}";

            while (nextLink != null)
            {
                var response = await HttpClient.GetAsync(nextLink);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Successfully called the next link");

                    var stringResponse = await response.Content.ReadAsStringAsync();

                    var jsonObject = JObject.Parse(stringResponse);

                    //the part of the response we want is in "value" which is an array of json data
                    var records = (JArray)jsonObject["value"];

                    if (records.Any())
                    {
                        Console.WriteLine("processing users");

                        await WriteToElastic(records);
                    }

                    //if there is more data then this link will be populated
                    nextLink = jsonObject.Value<string>("@odata.nextLink");
                }
                else
                {
                    Console.WriteLine($"Failed to call the web API: {response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();

                    // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                    // this is because the tenant admin as not granted consent for the application to call the Web API
                    Console.WriteLine($"Content: {content}");
                }
            }           
        }

        public async Task WriteToElastic(JArray users)
        {
            foreach (var user in users)
            {
                var userId = user.Value<string>("id");
                var userPrincipalName = user.Value<string>("userPrincipalName");

                //var asyncIndexResponse = await _elasticDbService.IndexAsync(_config.Users.IndexName, userId, user.ToString());

                //if (asyncIndexResponse != null && !asyncIndexResponse.Success)
                //{
                //    Console.WriteLine(asyncIndexResponse.Body);
                //}

                if (userPrincipalName == "xxxxxxxxxxxxx")
                    
                {
                    var chatMessagesEndpoint = $"{_config.ApiUrl}{_config.ChatMessages.Endpoint}";

                    //need to repeat here by picking up the next link as per users

                    var chatMessagesResponse = await HttpClient.GetAsync(chatMessagesEndpoint.Replace("{id}", userId));

                    if (chatMessagesResponse.IsSuccessStatusCode)
                    {
                        var chatMessagesStringResponse = await chatMessagesResponse.Content.ReadAsStringAsync();
                        var chatMessagesJsonObject = JObject.Parse(chatMessagesStringResponse);

                        //the part of the response we want is in "value" which is an array of json data
                        var chatMessagesRecords = (JArray)chatMessagesJsonObject["value"];

                        if (chatMessagesRecords.Any())
                        {
                            //chats that we want
                            Console.WriteLine($"writing chats for user {userPrincipalName}");

                            foreach (var chatMessageRecord in chatMessagesRecords)
                            {
                                var chatMessageId = chatMessageRecord.Value<string>("id");

                                chatMessageRecord["userPrincipalName"] = userPrincipalName;

                                //var asyncIndexResponse = await _elasticDbService.IndexAsync(_config.ChatMessages.IndexName, chatMessageId, chatMessageRecord.ToString());

                                //if (asyncIndexResponse != null && !asyncIndexResponse.Success)
                                //{
                                //    Console.WriteLine(asyncIndexResponse.Body);
                                //}

                                //pick up the chat details from chat endpoint
                                var chatId = chatMessageRecord.Value<string>("chatId");

                                var chatEndpoint = $"{_config.ApiUrl}{_config.Chat.Endpoint}";
                                var chatResponse = await HttpClient.GetAsync(chatEndpoint.Replace("{id}", chatId));

                                if (chatResponse.IsSuccessStatusCode)
                                {
                                    var chatStringResponse = await chatResponse.Content.ReadAsStringAsync();
                                    var chatJsonObject = JObject.Parse(chatStringResponse);
                                    var id = chatJsonObject.Value<string>("id");

                                    //asyncIndexResponse = await _elasticDbService.IndexAsync(_config.Chat.IndexName, id, chatJsonObject.ToString());

                                    //if (asyncIndexResponse != null && !asyncIndexResponse.Success)
                                    //{
                                    //    Console.WriteLine(asyncIndexResponse.Body);
                                    //}
                                }

                                //pick up the members of the chat from chat members endpoint
                                var chatMembersEndpoint = $"{_config.ApiUrl}{_config.ChatMembers.Endpoint}";
                                var chatMembersResponse = await HttpClient.GetAsync(chatMembersEndpoint.Replace("{id}", chatId));

                                if (chatMembersResponse.IsSuccessStatusCode)
                                {
                                    var chatMembersStringResponse = await chatMembersResponse.Content.ReadAsStringAsync();
                                    var chatMembersJsonObject = JObject.Parse(chatMembersStringResponse);

                                    //the part of the response we want is in "value" which is an array of json data
                                    var chatMembersRecords = (JArray)chatMembersJsonObject["value"];

                                    if (chatMembersRecords.Any())
                                    {
                                        foreach (var chatMembersRecord in chatMembersRecords)
                                        {
                                            var chatMembersId = chatMembersRecord.Value<string>("id");

                                            //asyncIndexResponse = await _elasticDbService.IndexAsync(_config.ChatMembers.IndexName, chatMembersId, chatMembersRecord.ToString());

                                            //if (asyncIndexResponse != null && !asyncIndexResponse.Success)
                                            //{
                                            //    Console.WriteLine(asyncIndexResponse.Body);
                                            //}
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to call the web API: {chatMessagesResponse.StatusCode}");

                        var content = await chatMessagesResponse.Content.ReadAsStringAsync();

                        // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                        // this is because the tenant admin as not granted consent for the application to call the Web API
                        Console.WriteLine($"Content: {content}");
                    }
                }
            }
        }
    }
}
