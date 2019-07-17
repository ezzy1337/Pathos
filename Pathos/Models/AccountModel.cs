using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("accounts")]
    public class Account {
        public int AccountId { get; set; }
        public string Username { get; set; }
        public string ProfileUrl { get; set; }

        //UserId is the Foreign Key to User.Id
        public int UserId { get; set; }
        //User is an Inverse Navigation Property
        public User User { get; set; }
    }
}
