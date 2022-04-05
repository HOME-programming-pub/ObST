using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models
{
    public class Author
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
