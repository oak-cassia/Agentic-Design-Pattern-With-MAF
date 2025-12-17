using System.ComponentModel;
using System.Text.Json.Serialization;

namespace CategorizationAgent.DTOs;

[Description("문의 분류 결과. 문의 내용에 적합한 카테고리 ID, 이름, 신뢰도, 이유, 키워드를 포함합니다.")]
public class ClassificationResult
{
    [JsonIgnore] public int InquiryId { get; set; }

    [JsonIgnore] public string InquiryDescription { get; set; } = string.Empty;

    [JsonPropertyName("category_id")]
    [Description("가장 적합한 카테고리의 ID (예: 1, 2, 99 등)")]
    public int CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    [Description("카테고리의 한국어 이름")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    [Description("분류 결과에 대한 확신 정도 (0.0 ~ 1.0)")]
    public double Confidence { get; set; }

    [JsonPropertyName("reason")]
    [Description("해당 카테고리로 분류한 구체적인 이유")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("keywords")]
    [Description("문의 내용에서 추출한 주요 키워드 목록")]
    public List<string> Keywords { get; set; } = new();

    [JsonIgnore] public string UserId { get; set; } = string.Empty;
}