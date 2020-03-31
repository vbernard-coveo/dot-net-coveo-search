using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

namespace dot_net_coveo_search
{
    class Program
    {
        const string API_KEY = "xxfcc10f89-5e0b-43d0-ae3c-c5976a418f70";
        const string COVEO_ORG_ID = "itemgrouping4a6ekmpe";
        const string SEARCH_ENDPOINT = "https://platform.cloud.coveo.com/rest/search/v2/";
        const string SEARCH_HUB = "dot-net-coveo-search";
        const string ANALYTICS_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/search?prioritizeVisitorParameter=false";
        const string VISIT_ENDPOINT = "https://platform.cloud.coveo.com/rest/ua/v15/analytics/visit?prioritizeVisitorParameter=false";
        class VisitDetail
        {
            public string visitId
            { get; set;}
            public string visitorId
            {
                get;
                set;
            }
            public string lastSearchId
            {
                get;
                set;
            }
            public string lastQuery
            {
                get;
                set;
            }
            public string lastAQ
            {
                get;
                set;
            }
        }
        class UsageAnalyticsBody
        {
            public string language { get; set; }
            public string queryText { get; set; }
            public string originLevel1 { get; set; }
            public string searchQueryUid { get; set; }
        }

        private static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            // Create new visit detail. This needs to be persisted in the user session.
            var VisitDetail = new VisitDetail();
            // Add the API KEY to the client headers
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + API_KEY);
            // Get the visit detail. Use a visitor id stored in the cookie for better personalization.
            await getVisitInformation(null);
            // Do a search request
            await search();
            // Leave a trace.
            await SendSearchUA();

            async Task getVisitInformation(string visitorId)
            {
                try
                {
                    var response = await client.GetAsync(VISIT_ENDPOINT + "?org=" + COVEO_ORG_ID);
                    response.EnsureSuccessStatusCode();
                    var responseString = response.Content.ReadAsStringAsync();
                    JObject responseJson = JObject.Parse(responseString.Result);
                    VisitDetail.visitId = responseJson["id"].ToString();
                    VisitDetail.visitorId = responseJson["visitorId"].ToString();
                    Console.WriteLine("User visit ID: " + VisitDetail.visitId);
                    Console.WriteLine("User visitor ID: " + VisitDetail.visitorId);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            };

            async Task search()
            {
                Console.WriteLine("Enter query:");
                // var query = Console.ReadLine();

                Dictionary<string, string> queryParams = new Dictionary<string, string>
                {
                    { "q", "red" },
                    { "aq", "" },
                    { "organizationId", COVEO_ORG_ID },
                    { "searchHub", SEARCH_HUB }
                };

                try
                {
                    var response = await client.PostAsync(SEARCH_ENDPOINT, new FormUrlEncodedContent(queryParams));
                    response.EnsureSuccessStatusCode();
                    var responseString = response.Content.ReadAsStringAsync();
                    JObject responseJson = JObject.Parse(responseString.Result);
                    VisitDetail.lastSearchId = responseJson["searchUid"].ToString();
                    Console.WriteLine("Results: " + responseJson["totalCount"]);
                    foreach (var item in responseJson["results"])
                    {
                        Console.WriteLine(item["title"].ToString() + item["uri"].ToString());
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }


            }

            async Task SendSearchUA()
            {
                var usageAnalyticsBody = new UsageAnalyticsBody();
                usageAnalyticsBody.language = "en";
                usageAnalyticsBody.searchQueryUid = VisitDetail.lastSearchId;
                usageAnalyticsBody.queryText = VisitDetail.lastQuery;
                usageAnalyticsBody.originLevel1 = "dot-net-search";

                var url = $"{ANALYTICS_ENDPOINT}&org={COVEO_ORG_ID}&visitor={VisitDetail.visitorId}";
                var body = new StringContent(JsonConvert.SerializeObject(usageAnalyticsBody), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(url, body);
                    response.EnsureSuccessStatusCode();
                    var responseString = response.Content.ReadAsStringAsync();
                    JObject responseJson = JObject.Parse(responseString.Result);
                    Console.WriteLine("UA send: " + responseJson);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }
    }
}