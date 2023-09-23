using SelenicSparkApp.Models;

namespace SelenicSparkApp.Views.Posts
{
    public class DetailsModel
    {
        public required Post Post { get; set; }
        public List<Comment>? Comments { get; set; }
    }
}
