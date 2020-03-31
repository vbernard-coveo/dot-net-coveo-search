using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dot_net_coveo_search {
    class Program {
        const string API_KEY = "";
        const string COVEO_ORG_ID = "itemgrouping4a6ekmpe";
        const string SEARCH_HUB = "dot-net-coveo-search";

        static async Task Main(string[] args) {

            // Create a Coveo Client
            var coveoClient = new CoveoClient(API_KEY, COVEO_ORG_ID);

            // Get the visit detail. Use a visitor id stored in the cookie for better personalization.
            var visitDetail = await coveoClient.GetVisitInformation(null);

            // Do a search request
            var lastQuery = await coveoClient.Search();

            // Leave a trace.
            await coveoClient.SendSearchUA();
        }

        public class CoveoClient {
            private const string SEARCH_ENDPOINT = "https://platform.cloud.coveo.com/rest/search/v2/";
            private const string ANALYTICS_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/search?prioritizeVisitorParameter=false";
            private const string VISIT_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/visit?prioritizeVisitorParameter=false";

            private static readonly HttpClient client = new HttpClient();

            public String orgId {get; private set;}

            private LastQuery lastQuery;
            private VisitDetail visitDetail;

            public CoveoClient(String apiKey, String orgId) {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                this.orgId = orgId;
            }

            public async Task<VisitDetail> GetVisitInformation(string visitorId) {
                try {
                    var response = await client.GetAsync(VISIT_ENDPOINT + "?org=" + this.orgId);
                    response.EnsureSuccessStatusCode();
                    var responseString = response.Content.ReadAsStringAsync();
                    JObject responseJson = JObject.Parse(responseString.Result);
                    var visitDetail = new VisitDetail();
                    visitDetail.visitId = responseJson["id"].ToString();
                    visitDetail.visitorId = responseJson["visitorId"].ToString();
                    Console.WriteLine("User visit ID: " + visitDetail.visitId);
                    Console.WriteLine("User visitor ID: " + visitDetail.visitorId);
                    this.visitDetail = visitDetail;
                    return visitDetail;
                } catch (HttpRequestException e) {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                    throw;
                }
            }

            public async Task<LastQuery> Search() {
                Console.WriteLine("Enter query:");
                // var query = Console.ReadLine();

                Dictionary<string, string> queryParams = new Dictionary<string, string> { 
                    { "q", "red" },
                    { "aq", "" },
                    { "organizationId", this.orgId },
                    { "searchHub", SEARCH_HUB }
                };

                try {
                    var response = await client.PostAsync(SEARCH_ENDPOINT, new FormUrlEncodedContent(queryParams));
                    response.EnsureSuccessStatusCode();
                    var responseString = response.Content.ReadAsStringAsync();
                    JObject responseJson = JObject.Parse(responseString.Result);
                    var lastQuery = new LastQuery {
                        searchid = responseJson["searchUid"].ToString(),
                        keyword =  "my Keyword"
                    };
                    lastQuery.aq = "";
                    lastQuery.responseTime = 666;
                    Console.WriteLine("Results: " + responseJson["totalCount"]);
                    foreach (var item in responseJson["results"]) {
                        Console.WriteLine(item["title"].ToString() + item["uri"].ToString());
                    }
                    this.lastQuery = lastQuery;
                    return lastQuery;
                } catch (HttpRequestException e) {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                    throw;
                }
            }

            public async Task SendSearchUA() {
                var usageAnalyticsBody = new UsageAnalyticsBody();
                usageAnalyticsBody.language = "en";
                usageAnalyticsBody.searchQueryUid = this.lastQuery.searchid;
                usageAnalyticsBody.queryText = this.lastQuery.keyword;
                usageAnalyticsBody.originLevel1 = "dot-net-search";
                usageAnalyticsBody.actionCause = "interfaceLoad";
                usageAnalyticsBody.actionType = "interface";
                usageAnalyticsBody.responseTime = this.lastQuery.responseTime;

                var url = $"{ANALYTICS_ENDPOINT}&org={this.orgId}&visitor={this.visitDetail.visitId}";
                var body = new StringContent(JsonConvert.SerializeObject(usageAnalyticsBody), Encoding.UTF8, "application/json");

                try {
                    var response = await client.PostAsync(url, body);
                    var responseString = response.Content.ReadAsStringAsync();
                    response.EnsureSuccessStatusCode();
                    JObject responseJson = JObject.Parse(responseString.Result);
                    Console.WriteLine("UA send: " + responseJson);
                } catch (HttpRequestException e) {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }
    }
}