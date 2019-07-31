using System.ComponentModel.DataAnnotations.Schema;

namespace Pathos.Models
{
    [Table("user_badges")] //creates the join table as user_badges
    public class UserBadges { // I refer to this as a join entity but truthfully it's just another entity
    //Foreign Key to User table
    public int UserId { get; set; }
    //Reference Navigation Property to User
    public User User { get; set; }

    //FK to Badge table
    public int BadgeId { get; set; }
    //Reference Navigation Property to Badge
    public Badge Badge { get; set; }
    }
}
