using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CategorizationAgent.Data.Models;

[Table("user_numbers")]
public class UserNumber
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    [StringLength(20)]
    public string UserId { get; set; } = string.Empty;
}

