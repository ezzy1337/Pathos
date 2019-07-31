using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("badges")]
    public class Badge
    {
        public int BadgeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        //Collection Navigation Property to UserBadges join entity
        public List<UserBadges> BadgeRecipients { get; set; }
    }
}
