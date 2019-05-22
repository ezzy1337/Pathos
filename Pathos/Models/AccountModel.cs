using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("accounts")]
    public class Account {
        public int AccountId { get; set; }
        public string Username { get; set; }
        public string ProfileUrl { get; set; }
    }
}
