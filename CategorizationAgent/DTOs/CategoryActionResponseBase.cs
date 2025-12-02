namespace CategorizationAgent.DTOs;

/// <summary>
/// 카테고리별 액션 실행 결과에 대한 기본 응답 클래스
/// 분류 후 각 카테고리에 맞는 처리 결과를 나타냅니다.
/// </summary>
public abstract class CategoryActionResponseBase
{
    /// <summary>
    /// 원본 문의 ID
    /// </summary>
    public int InquiryId { get; set; }
    
    /// <summary>
    /// 사용자 ID (문자열)
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 사용자 번호 ID
    /// </summary>
    public int UserNumberId { get; set; }
    
    /// <summary>
    /// 답변 메시지
    /// </summary>
    public string ResponseMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 처리 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }
}

