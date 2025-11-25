using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;

namespace CategorizationAgent.Executors;

/// <summary>
/// CSV 파일에서 문의 데이터를 읽어오는 간단한 Executor
/// </summary>
public class SimpleInquiryReadExecutor
{
    private readonly string _filePath;

    public SimpleInquiryReadExecutor(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"CSV 파일을 찾을 수 없습니다: {_filePath}");
        }
    }

    /// <summary>
    /// CSV 파일에서 모든 문의를 읽어옵니다.
    /// </summary>
    public async Task<List<Inquiry>> ReadAllInquiriesAsync()
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(_filePath);
            var inquiries = new List<Inquiry>();

            // 첫 번째 줄은 헤더이므로 건너뜀
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var inquiry = ParseCsvLine(line);
                if (inquiry != null)
                {
                    inquiries.Add(inquiry);
                }
            }

            return inquiries;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"CSV 파일 읽기 실패: {_filePath}", ex);
        }
    }

    private Inquiry? ParseCsvLine(string line)
    {
        try
        {
            // CSV 형식: id,userId,description,status
            var parts = SplitCsvLine(line);
            
            if (parts.Length < 4)
                return null;

            return new Inquiry
            {
                Id = int.Parse(parts[0]),
                UserId = parts[1],
                Description = parts[2],
                Status = Enum.Parse<InquiryStatus>(parts[3], ignoreCase: true),
                Category = InquiryCategory.Uncategorized,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        catch
        {
            return null;
        }
    }

    private string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());
        return result.ToArray();
    }

    /// <summary>
    /// 특정 상태의 문의만 필터링하여 읽어옵니다.
    /// </summary>
    public async Task<List<Inquiry>> ReadInquiriesByStatusAsync(InquiryStatus status)
    {
        var allInquiries = await ReadAllInquiriesAsync();
        return allInquiries.Where(i => i.Status == status).ToList();
    }

    /// <summary>
    /// 특정 사용자의 문의만 필터링하여 읽어옵니다.
    /// </summary>
    public async Task<List<Inquiry>> ReadInquiriesByUserIdAsync(string userId)
    {
        var allInquiries = await ReadAllInquiriesAsync();
        return allInquiries.Where(i => i.UserId == userId).ToList();
    }
}

