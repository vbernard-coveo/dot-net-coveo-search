class SearchAnalyticBody
{
    public string actionCause { get; set; }
    public string actionType { get; set; }
    public bool anonymous { get; set; }
    public string device { get; set; }
    public string username { get; set; }
    public string userDisplayName { get; set; }
    public bool mobile { get; set; }
    public string language { get; set; }
    public int responseTime { get; set; }
    public string originLevel1 { get; set; }
    public string originLevel2 { get; set; }
    public string originLevel3 { get; set; }
    public string originContext { get; set; }
    public string userAgent { get; set; }
    public string searchQueryUid { get; set; }
    public string queryText { get; set; }
    public string advancedQuery { get; set; }
    public int resultsPerPage { get; set; }
    public int pageNumber { get; set; }
    public bool didYouMean { get; set; }
    public bool contextual { get; set; }
    public string queryPipeline { get; set; }
    public int numberOfResults { get; set; }
}