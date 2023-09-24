using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SelenicSparkApp.Models
{
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate INT on creation, not required by some DBMS's
        public required int PostId { get; set; }
        public required string Title { get; set; }
        public string? Text { get; set; }
        public required string Author { get; set; }
        public required DateTimeOffset CreatedDate { get; set; }

        public Post() {}
    }
}
