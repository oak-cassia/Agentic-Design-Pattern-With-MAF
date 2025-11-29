namespace CategorizationAgent.DTOs;

/// <summary>
/// 문의 분류 결과를 담는 DTO
/// </summary>
public class ClassificationResult
{
    /// <summary>
    /// 원본 문의 ID
    /// </summary>
    public int InquiryId { get; set; }
    
    /// <summary>
    /// 카테고리 ID (1-8, 98: 복합문의, 99: 기타)
    /// </summary>
    public int CategoryId { get; set; }
    
    /// <summary>
    /// 카테고리 한글명
    /// </summary>
    public string CategoryNameKo { get; set; } = string.Empty;
    
    /// <summary>
    /// 카테고리 영문명
    /// </summary>
    public string CategoryNameEn { get; set; } = string.Empty;
    
    /// <summary>
    /// 분류 신뢰도 (0.0 ~ 1.0)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// 복합 문의 여부
    /// </summary>
    public bool IsMultiLabel { get; set; }
    
    /// <summary>
    /// 하위 카테고리 리스트 (복합 문의인 경우)
    /// </summary>
    public List<string> SubCategories { get; set; } = new();
    
    /// <summary>
    /// 분류 이유
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 추출된 키워드
    /// </summary>
    public List<string> Keywords { get; set; } = new();
}
