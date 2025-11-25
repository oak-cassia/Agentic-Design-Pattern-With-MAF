using CategorizationAgent.Enums;

namespace CategorizationAgent.DTOs;

public class Inquiry
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? Description { get; set; }
    public InquiryCategory? Category { get; set; }
    public InquiryStatus? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}