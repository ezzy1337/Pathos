using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("user_badges")]
    public class UserBadges {
        public int UserId { get; set; }
        public User User { get; set; }

        public int BadgeId { get; set; }
        public Badge Badge { get; set; }
    }
}
