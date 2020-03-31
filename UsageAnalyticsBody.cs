class UsageAnalyticsBody
{
    public string language { get; set; }
    public string queryText { get; set; }
    public string originLevel1 { get; set; }
    public string searchQueryUid { get; set; }
    public string actionCause { get; set; }
    public string actionType { get; set; }
    public int responseTime { get; set; }
}