using System.ComponentModel.DataAnnotations;

namespace SelenicSparkApp.Models
{
    /// <summary>
    /// Expands Post fields, binded to Post migration-database; DOES NOT inherit the original class.
    /// </summary>
    public class PostComments
    {
        [Key]
        public required string CommentId {  get; set; }
        public Post PostId { get; set; } // Only PostId would be stored here, unless it's 'null'
        public required string Text { get; set; }
        public required string Author { get; set; }
        public required DateTimeOffset CreatedDate { get; set; }

        public PostComments()
        {
            // Default constructor. Delete this and all migration builds will start failing.
        }

        public PostComments(string cid, Post pid, string text, string author, DateTimeOffset createdDate)
        {
            CommentId = cid;
            PostId = pid;
            Text = text;
            Author = author;
            CreatedDate = createdDate;
        }
    }
}
