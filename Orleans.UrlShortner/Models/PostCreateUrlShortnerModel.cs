using System.ComponentModel.DataAnnotations;

namespace Orleans.UrlShortner.Models;

public class PostCreateUrlShortnerModel
{
    public class Request
    {
        [Required] public string Url { get; set; }
        public bool? IsOneShoot { get; set; }
        public int? DurationInSeconds { get; set; }
    }

    public class Response
    {
        public Uri Url { get; set; }
    }
}
