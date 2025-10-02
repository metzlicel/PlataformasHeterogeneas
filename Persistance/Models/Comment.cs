using System.ComponentModel.DataAnnotations;

namespace Blog.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        /// <summary>
        /// The identifier of the article this comment belongs to.
        /// </summary>
        public int ArticleId { get; set; }

        /// <summary>
        /// The content of the comment.
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// Represents the moment the comment was posted.
        /// </summary>
        public DateTimeOffset PublishedDate { get; set; }
        
        public Article Article { get; set; }

    }
}