using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;
using System.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace CategorizationAgent.Executors;

// ReflectingExecutor를 상속받고, IMessageHandler 인터페이스를 명시적으로 구현합니다.
public class SimpleInquiryReadExecutor(string filePath) : ReflectingExecutor<SimpleInquiryReadExecutor>("SimpleInquiryReadExecutor"),
    IMessageHandler<string, List<Inquiry>>,
    IMessageHandler<List<ChatMessage>, List<Inquiry>>
{
    public async ValueTask<List<Inquiry>> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return await ReadInquiriesFromCsvAsync(cancellationToken);
    }

    public async ValueTask<List<Inquiry>> HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return await ReadInquiriesFromCsvAsync(cancellationToken);
    }

    #region Private Helper Methods

    private async ValueTask<List<Inquiry>> ReadInquiriesFromCsvAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("SimpleInquiryReadExecutor Start");

        // 1. 입력값(파일 경로) 유효성 검사
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath), "CSV 파일 경로가 설정되지 않았습니다.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV 파일을 찾을 수 없습니다: {filePath}");
        }

        // 2. 비동기 파일 읽기
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            var inquiries = new List<Inquiry>();

            // 3. 파싱 로직
            for (int i = 1; i < lines.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var inquiry = ParseCsvLine(line);
                if (inquiry != null)
                {
                    inquiries.Add(inquiry);
                }
            }

            return inquiries;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"CSV 처리 중 오류 발생: {filePath}", ex);
        }
    }

    private Inquiry? ParseCsvLine(string line)
    {
        try
        {
            var parts = SplitCsvLine(line);
            if (parts.Length < 4) return null;

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
        var currentField = new StringBuilder();
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

    #endregion
}