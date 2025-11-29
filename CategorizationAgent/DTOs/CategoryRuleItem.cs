using System.Text.Json.Serialization;

namespace CategorizationAgent.Agents;

class CategoryRuleItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("handling_summary")]
    public string HandlingSummary { get; set; } = string.Empty;
}