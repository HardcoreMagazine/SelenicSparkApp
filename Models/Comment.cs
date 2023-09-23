using System.ComponentModel.DataAnnotations;

namespace SelenicSparkApp.Models
{
    /// <summary>
    /// Expands Post fields, binded to Post migration-database; DOES NOT inherit the original class.
    /// </summary>
    public class Comment
    {
        [Key]
        public string CommentId {  get; set; }
        public required int PostId { get; set; }
        public required string Text { get; set; }
        public required string Author { get; set; }
        public required DateTimeOffset CreatedDate { get; set; }

        public Comment() {}

        /*public Comment()
        {
            // Default constructor. Delete this and all migration builds will start failing.
        }

        public Comment(string cid, int pid, string text, string author, DateTimeOffset createdDate)
        {
            CommentId = cid;
            PostId = pid;
            Text = text;
            Author = author;
            CreatedDate = createdDate;
        }*/
    }
}
