using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace dot_net_coveo_search
{
    class Program
    {

        // MAIN SETTINGS
        const string API_KEY = "";
        const string COVEO_ORG_ID = "";
        const string SEARCH_HUB = "";
        const string FACET = "";
        static async Task Main(string[] args)
        {
            // Create a Coveo Client
            var coveoClient = new CoveoClient(API_KEY, COVEO_ORG_ID);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🟢 Dot Net Client Initialized for " + COVEO_ORG_ID);
            Console.WriteLine(" ");

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
                    
                    // Perform another search?
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Continue session? (Y/N)");
                    continueSession = Console.ReadLine();
                }
            // Terminate the session.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("🟡 Coveo Client Terminated");
        }
        public class CoveoClient
        {
            private const string SEARCH_ENDPOINT = "https://platform.cloud.coveo.com/rest/search/v2/";
            private const string QUERY_SUGGEST_ENDPOINT = "https://platform.cloud.coveo.com/rest/search/v2/querySuggest";
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
                    // DEBUG tip: Remove comment from the line below and inspect the more verbose messages from Coveo Cloud
                    var responseString = response.Content.ReadAsStringAsync();
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
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Creating Visit");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("User visit ID: " + visitDetail.visitId);
                    Console.WriteLine("User visitor ID: " + visitDetail.visitorId);
                    Console.WriteLine(" ");

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
                // var userQuery = "laptop";
                Console.WriteLine("Enter filters:");
                var userAdvancedQuery = Console.ReadLine();
                // var userAdvancedQuery = "";

                // Build Facets
                var facet = new GroupByParams{
                    field = FACET,
                    maximumNumberOfValues = 10,
                    sortCriteria = "occurrences",
                    injectionDepth = 1000,
                    completeFacetWithStandardValues = true,
                };

                GroupByParams[] facets = new GroupByParams[1];
                facets.SetValue(facet, 0);

                // Build Params
                var queryParam = new QueryParams{
                    q = userQuery,
                    aq = userAdvancedQuery,
                    organizationId = this.orgId,
                    searchHub = SEARCH_HUB,
                    groupBy = facets
                };

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

                // Print Query Info
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Query Success");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Results: {responseJson["totalCount"]}           Response Time: {responseJson["duration"]}ms");
                Console.WriteLine($" ");    

                // Print Facet Info
                foreach (var facetResult in responseJson["groupByResults"]){
                    Console.WriteLine($"Filter {facetResult["field"]}");
                        foreach (var facetValue in facetResult["values"]){
                            Console.WriteLine($"{facetValue["numberOfResults"]} - {facetValue["value"]} ");
                        }
                }
                Console.WriteLine($" ");


                // Print Result List
                Console.ForegroundColor = ConsoleColor.White;
                var i = 1;
                foreach (var result in responseJson["results"])
                {
                    Console.WriteLine($"{i} | {result["raw"]["coveodpunifiedbrand"].ToString()} | {result["title"].ToString()} | ${result["raw"]["dpretailprice"]} ➜ ${result["raw"]["dpsaleprice"]}");
                    Console.WriteLine($" ");
                    i++;
                }
                
                // Return last query for UA
                this.lastQuery = lastQuery;
                return lastQuery;
            }

            public async Task SendSearchUA()
            {

                // Build Analytic Message
                var usageAnalyticsBody = new SearchAnalyticBody{
                    language = "en",
                    queryText = this.lastQuery.keyword,
                    originLevel1 = SEARCH_HUB,
                    actionCause = "searchboxSubmit",
                    actionType = "search box",
                    originContext = "Search",
                    username = "vbernard@coveo.com",
                    userDisplayName = "Vincent",
                    searchQueryUid = this.lastQuery.searchid,
                    responseTime = this.lastQuery.responseTime,
                    advancedQuery = this.lastQuery.aq,
                    resultsPerPage = 10,
                    numberOfResults = this.lastQuery.totalCount,
                };

                // Build Request
                var url = $"{ANALYTICS_ENDPOINT}&org={this.orgId}&visitor={this.visitDetail.visitorId}";
                var body = new StringContent(JsonConvert.SerializeObject(usageAnalyticsBody), Encoding.UTF8, "application/json");

                // Get Response
                var response = await PostAsync(url, body);
                var responseString = response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(responseString.Result);

                // Print Analytics Details
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Search Event Sent: {this.lastQuery.keyword}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("UA send: " + responseJson);
                Console.WriteLine(" ");

            }

            public async Task SendClickUA(){
                var selectedResult = "";
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Click on? [1-10] or N");
                selectedResult = Console.ReadLine();
                // selectedResult = "1";

                if(selectedResult == "N"){
                    return;
                }

                // Print Selected Result
                var selectedResultPosition = int.Parse(selectedResult);
                var resultPositionInResponse = selectedResultPosition -1;
                var result = this.lastQuery.results["results"][resultPositionInResponse];
                Console.WriteLine($"{result["title"]}");

                // Build Click Message
                var usageAnalyticsBody = new ClickAnalyticBody{
                    language = "en",
                    originLevel1 = SEARCH_HUB,
                    actionCause = "documentOpen",
                    actionType = "document",
                    originContext = "Search",
                    username = "vbernard@coveo.com",
                    userDisplayName = "Vincent",
                    searchQueryUid = this.lastQuery.searchid,
                    documentPosition = selectedResultPosition,
                    documentTitle = result["title"].ToString(),
                    documentUrl = result["clickUri"].ToString(),
                    documentUri = result["uri"].ToString(),
                    documentUriHash = result["raw"]["urihash"].ToString(),
                    sourceName = result["raw"]["source"].ToString(),
                    queryPipeline = this.lastQuery.results["pipeline"].ToString()
                };

                // Add validation for rankingModidier, as the attribute is not always on the results
                var rankingModifier = result["rankingModifier"];
                if ( rankingModifier != null ){
                    usageAnalyticsBody.rankingModifier = result["rankingModifier"].ToString();
                }

                // Build Request
                var url = $"{CLICK_ENDPOINT}&org={this.orgId}&visitor={this.visitDetail.visitorId}";
                var body = new StringContent(JsonConvert.SerializeObject(usageAnalyticsBody), Encoding.UTF8, "application/json");


                // Get Response
                var response = await PostAsync(url, body);
                var responseString = response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(responseString.Result);

                // Print Analytics Details
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Click Event Sent: {selectedResult} | { result["title"]}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("UA send: " + responseJson);
                Console.WriteLine(" ");
            }
        }
    }
}