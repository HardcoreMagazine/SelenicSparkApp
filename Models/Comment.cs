using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SelenicSparkApp.Models
{
    /// <summary>
    /// Expands Post fields, binded to Post migration-database; DOES NOT inherit the original class.
    /// </summary>
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate INT on creation, not required by some DBMS's
        public int CommentId {  get; set; }
        public required int PostId { get; set; }
        public required string Text { get; set; }
        public required string Author { get; set; }
        public required DateTimeOffset CreatedDate { get; set; }

        public Comment() {}
    }
}
