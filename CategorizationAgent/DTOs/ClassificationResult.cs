using System.ComponentModel;
using System.Text.Json.Serialization;

namespace CategorizationAgent.DTOs;

/// <summary>
/// 문의 분류 결과를 담는 DTO
/// </summary>
[Description("문의 분류 결과. 문의 내용에 적합한 카테고리 ID, 이름, 신뢰도, 이유, 키워드를 포함합니다.")]
public class ClassificationResult
{
    /// <summary>
    /// 원본 문의 ID (입력값에서 복사됨)
    /// </summary>
    [JsonIgnore] // AI가 생성하지 않고 Executor에서 할당
    public int InquiryId { get; set; }
    
    /// <summary>
    /// 원본 문의 내용 (입력값에서 복사됨)
    /// </summary>
    [JsonIgnore] // AI가 생성하지 않고 Executor에서 할당
    public string InquiryDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 카테고리 ID (1-8, 98: 복합문의, 99: 기타)
    /// </summary>
    [JsonPropertyName("category_id")]
    [Description("가장 적합한 카테고리의 ID (예: 1, 2, 99 등)")]
    public int CategoryId { get; set; }
    
    /// <summary>
    /// 카테고리명
    /// </summary>
    [JsonPropertyName("category_name")]
    [Description("카테고리의 한국어 이름")]
    public string CategoryName { get; set; } = string.Empty;
    
    /// <summary>
    /// 분류 신뢰도 (0.0 ~ 1.0)
    /// </summary>
    [JsonPropertyName("confidence")]
    [Description("분류 결과에 대한 확신 정도 (0.0 ~ 1.0)")]
    public double Confidence { get; set; }
    
    /// <summary>
    /// 분류 이유
    /// </summary>
    [JsonPropertyName("reason")]
    [Description("해당 카테고리로 분류한 구체적인 이유")]
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 추출된 키워드
    /// </summary>
    [JsonPropertyName("keywords")]
    [Description("문의 내용에서 추출한 주요 키워드 목록")]
    public List<string> Keywords { get; set; } = new();
}