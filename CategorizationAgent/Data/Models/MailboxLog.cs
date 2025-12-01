using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CategorizationAgent.Data.Models;

[Table("mailbox_logs")]
public class MailboxLog
{
    [Key]
    public long Id { get; set; }
        
    [Column("user_id")]
    public long UserId { get; set; }
        
    [Column("message_id")]
    public string MessageId { get; set; } = string.Empty;
        
    [Column("item_type")]
    public int ItemType { get; set; }
        
    [Column("item_id")]
    public int ItemId { get; set; }
        
    // 0: 수령전, 1: 수령완료, 2: 만료/삭제
    [Column("mail_state")]
    public int MailState { get; set; } 
        
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}