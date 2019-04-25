using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("users")]
    public class User {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
