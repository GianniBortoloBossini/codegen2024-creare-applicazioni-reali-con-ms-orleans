namespace Orleans.UrlShortner.Models;

public class GetStatisticsModel
{
    public class Response
    {
        public int TotalActivations { get; set; }
        public int TotalActiveShortenedRouteSegment { get; set; }
    }
}
