using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dot_net_coveo_search
{
    class Program
    {

        // MAIN SETTINGS
        const string API_KEY = "";
        const string COVEO_ORG_ID = "";
        const string SEARCH_HUB = "";
        static async Task Main(string[] args)
        {
            // Create a Coveo Client
            var coveoClient = new CoveoClient(API_KEY, COVEO_ORG_ID);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🟢 Coveo Client Initialized for " + COVEO_ORG_ID);
            
            // Get the visit detail. Use a visitor id stored in the cookie for better personalization.
            var visitDetail = await coveoClient.GetVisitInformation(null);
            var continueSession = "Y";

                while(continueSession == "Y"){
                // Do a search request
                var lastQuery = await coveoClient.Search();

                // Leave a trace.
                await coveoClient.SendSearchUA();

                // Send a click
                await coveoClient.SendClickUA();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Continue session? (Y/N)");
                continueSession = Console.ReadLine();
                }
                
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("🟡 Coveo Client Terminated");
        }

        public class CoveoClient
        {
            private const string SEARCH_ENDPOINT = "https://platform.cloud.coveo.com/rest/search/v2/";
            private const string ANALYTICS_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/search?prioritizeVisitorParameter=false";
            private const string CLICK_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/click?prioritizeVisitorParameter=false";
            private const string VISIT_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/visit?prioritizeVisitorParameter=false";
            private static readonly HttpClient client = new HttpClient();
            public String orgId { get; private set; }
            private LastQuery lastQuery;
            private VisitDetail visitDetail;

            public CoveoClient(String apiKey, String orgId)
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                this.orgId = orgId;
            }

            public async Task<HttpResponseMessage> PostAsync(string url, StringContent body)
            {
                try
                {
                    var response = await client.PostAsync(url, body);
                    response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                    throw;
                }
            }
            public async Task<VisitDetail> GetVisitInformation(string visitorId)
            {
                try
                {
                    // Create Request
                    var url = $"{VISIT_ENDPOINT}?org={this.orgId}&visitor={visitorId}";
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var responseString = response.Content.ReadAsStringAsync();
                    JObject responseJson = JObject.Parse(responseString.Result);

                    // Create Request
                    var visitDetail = new VisitDetail();
                    visitDetail.visitId = responseJson["id"].ToString();
                    visitDetail.visitorId = responseJson["visitorId"].ToString();

                    // Print Visit information
                    Console.WriteLine("Fetching Visit Information");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("User visit ID: " + visitDetail.visitId);
                    Console.WriteLine("User visitor ID: " + visitDetail.visitorId);

                    this.visitDetail = visitDetail;
                    return visitDetail;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                    throw;
                }
            }

            public async Task<LastQuery> Search()
            {
                // Get user input
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Performing Search");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Enter keywords: ");
                var userQuery = Console.ReadLine();
                Console.WriteLine("Enter filters:");
                var userAdvancedQuery = Console.ReadLine();

                // Build Params
                var queryParam = new QueryParam();
                queryParam.q = userQuery;
                queryParam.aq = userAdvancedQuery;
                queryParam.organizationId = this.orgId;
                queryParam.searchHub = SEARCH_HUB;

                // Build Request
                var url = SEARCH_ENDPOINT;
                var body = new StringContent(JsonConvert.SerializeObject(queryParam), Encoding.UTF8, "application/json");

                // Get Response
                var response = await PostAsync(url, body);
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(responseString.Result);

                // Store Query Info
                var lastQuery = new LastQuery
                {
                    searchid = responseJson["searchUid"].ToString(),
                    keyword = queryParam.q,
                    aq = queryParam.aq,
                    responseTime = int.Parse(responseJson["duration"].ToString()),
                    totalCount = int.Parse(responseJson["totalCount"].ToString()),
                    results = responseJson
                };

                // Print Result Info
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Query Success");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Results: " + responseJson["totalCount"]);
                var i = 0;
                foreach (var item in responseJson["results"])
                {
                    Console.WriteLine($"{i} | {item["title"].ToString()} | ${item["raw"]["dpretailprice"]} | {item["uri"].ToString()}");
                    i++;
                }
                this.lastQuery = lastQuery;
                return lastQuery;
            }

            public async Task SendSearchUA()
            {

                // Build Analytic Message
                var usageAnalyticsBody = new SearchAnalyticBody();
                usageAnalyticsBody.language = "en";
                usageAnalyticsBody.queryText = this.lastQuery.keyword;
                usageAnalyticsBody.originLevel1 = SEARCH_HUB;
                usageAnalyticsBody.actionCause = "searchboxSubmit";
                usageAnalyticsBody.actionType = "search box";
                usageAnalyticsBody.originContext = "Search";
                usageAnalyticsBody.username = "vbernard@coveo.com";
                usageAnalyticsBody.userDisplayName = "Vincent";
                usageAnalyticsBody.searchQueryUid = this.lastQuery.searchid;
                usageAnalyticsBody.responseTime = this.lastQuery.responseTime;
                usageAnalyticsBody.advancedQuery = this.lastQuery.aq;
                usageAnalyticsBody.resultsPerPage = 10;
                usageAnalyticsBody.numberOfResults = this.lastQuery.totalCount;

                // Build Request
                var url = $"{ANALYTICS_ENDPOINT}&org={this.orgId}&visitor={this.visitDetail.visitorId}";
                var body = new StringContent(JsonConvert.SerializeObject(usageAnalyticsBody), Encoding.UTF8, "application/json");

                // Get Response
                var response = await PostAsync(url, body);
                var responseString = response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(responseString.Result);

                // Print Analytics Details
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Sending UA Search Event");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("UA send: " + responseJson);

            }

            public async Task SendClickUA(){
                var selectedResult = "";
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Click on? [0-9] or N");
                selectedResult = Console.ReadLine();
                
                if(selectedResult == "N"){
                    return;
                }
                // Print Selected Result
                Console.WriteLine($"{this.lastQuery.results["results"][0]["title"]}");
                var selectedResultPosition = int.Parse(selectedResult);

                // Build Click Message
                var usageAnalyticsBody = new ClickAnalyticBody();
                var result = this.lastQuery.results["results"][selectedResultPosition];
                usageAnalyticsBody.language = "en";
                usageAnalyticsBody.originLevel1 = SEARCH_HUB;
                usageAnalyticsBody.actionCause = "searchboxSubmit";
                usageAnalyticsBody.originContext = "Search";
                usageAnalyticsBody.username = "vbernard@coveo.com";
                usageAnalyticsBody.userDisplayName = "Vincent";
                usageAnalyticsBody.searchQueryUid = this.lastQuery.searchid;
                usageAnalyticsBody.documentPosition = selectedResultPosition;
                usageAnalyticsBody.documentTitle = result["title"].ToString();
                usageAnalyticsBody.documentUrl = result["uri"].ToString();
                usageAnalyticsBody.documentUri = result["uri"].ToString();
                usageAnalyticsBody.documentUriHash = result["raw"]["urihash"].ToString();
                usageAnalyticsBody.sourceName = result["raw"]["source"].ToString();
                var rankingModifier = result["rankingModifier"];
                if ( rankingModifier != null ){
                    usageAnalyticsBody.rankingModifier = result["rankingModifier"].ToString();
                }
                usageAnalyticsBody.queryPipeline = this.lastQuery.results["pipeline"].ToString();

                // Build Request
                var url = $"{CLICK_ENDPOINT}&org={this.orgId}&visitor={this.visitDetail.visitorId}";
                var body = new StringContent(JsonConvert.SerializeObject(usageAnalyticsBody), Encoding.UTF8, "application/json");


                // Get Response
                var response = await PostAsync(url, body);
                var responseString = response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(responseString.Result);

                // Print Analytics Details
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Sending UA Search Event");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("UA send: " + responseJson);
            }
        }
    }
}