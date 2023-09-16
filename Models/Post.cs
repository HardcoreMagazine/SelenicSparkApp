namespace SelenicSparkApp.Models
{
    public class Post
    {
        public required int PostId { get; set; }
        public required string Title { get; set; }
        public string? Text { get; set; }
        public required string Author { get; set; }
        public required DateTimeOffset CreatedDate { get; set; }

        public Post() {}
    }
}
