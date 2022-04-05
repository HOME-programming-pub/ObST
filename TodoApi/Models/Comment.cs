using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApi.Models
{
    public class Comment
    {

        [Key]
        public long Id { get; set; }

        [ForeignKey(nameof(TodoItemDBModel))]
        public long TodoItemId { get; set; }

        [ForeignKey(nameof(Author))]
        public long AuthorId { get; set; }

        public string Message { get; set; }
    }

    public class CommentInput
    {
        [Required]
        public long AuthorId { get; set; }

        [Required]
        public string Message { get; set; }
    }

}
