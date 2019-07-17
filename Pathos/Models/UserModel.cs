using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("users")]
    public class User {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public List<Account> Accounts { get; set; }

        public List<UserBadges> Badges { get; set; }
    }
}
