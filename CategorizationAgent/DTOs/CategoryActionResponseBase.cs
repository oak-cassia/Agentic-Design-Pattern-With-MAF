namespace CategorizationAgent.DTOs;

public abstract class CategoryActionResponseBase
{
    public int InquiryId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int UserNumberId { get; set; }

    public string ResponseMessage { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }
}