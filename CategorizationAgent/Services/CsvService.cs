using System.Text;
using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;

namespace CategorizationAgent.Services;

/// <summary>
/// CSV 파일 읽기/쓰기 및 파싱을 담당하는 서비스
/// </summary>
public class CsvService
{
    // 동시성 제어를 위한 세마포어 (싱글톤이므로 static이 아니어도 되지만, 파일 잠금을 위해 인스턴스 필드로 사용)
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// CSV 파일에서 Inquiry 목록을 읽어옵니다.
    /// </summary>
    public async Task<List<Inquiry>> ReadInquiriesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ValidateFilePath(filePath);

        await _fileLock.WaitAsync(cancellationToken); // 잠금 획득
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            var inquiries = new List<Inquiry>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var inquiry = ParseInquiryFromCsvLine(line);
                if (inquiry != null)
                {
                    inquiries.Add(inquiry);
                }
            }
            Console.WriteLine($"[CsvService] {inquiries.Count}개의 문의를 읽었습니다.");
            return inquiries;
        }
        finally
        {
            _fileLock.Release(); // 잠금 해제
        }
    }

    /// <summary>
    /// CSV 파일의 특정 Inquiry를 업데이트합니다.
    /// </summary>
    public async Task<bool> UpdateInquiriesAsync(
        string filePath,
        Dictionary<int, (InquiryStatus Status, string ResponseMessage)> updates,
        CancellationToken cancellationToken = default)
    {
        ValidateFilePath(filePath);

        await _fileLock.WaitAsync(cancellationToken); // 잠금 획득
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            if (lines.Length == 0) throw new InvalidOperationException("CSV 파일이 비어있습니다.");

            var updatedLines = new List<string>(lines.Length);
            updatedLines.Add(lines[0]); // 헤더 유지
            
            int updatedCount = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    updatedLines.Add(line);
                    continue;
                }

                var updatedLine = UpdateCsvLine(line, updates, ref updatedCount);
                updatedLines.Add(updatedLine);
            }

            await File.WriteAllLinesAsync(filePath, updatedLines, cancellationToken);
            Console.WriteLine($"[CsvService] {updatedCount}개의 레코드를 업데이트했습니다.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CsvService] 오류 발생: {ex.Message}");
            throw; // 예외를 삼키지 않고 상위로 전파
        }
        finally
        {
            _fileLock.Release(); // 잠금 해제
        }
    }

    #region Private Helper Methods

    private void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CSV 파일을 찾을 수 없습니다: {filePath}");
    }

    private Inquiry? ParseInquiryFromCsvLine(string line)
    {
        try
        {
            var parts = SplitCsvLine(line);
            if (parts.Count < 4) return null; // List<string> 반환으로 변경했으므로 Count 사용

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

    private string UpdateCsvLine(
        string line,
        Dictionary<int, (InquiryStatus Status, string ResponseMessage)> updates,
        ref int updatedCount)
    {
        try
        {
            var parts = SplitCsvLine(line); // List<string> 반환
            
            // [수정] 컬럼이 4개뿐일 경우(응답 없음) 5번째 컬럼 추가를 위해 패딩
            while (parts.Count < 5)
            {
                parts.Add("");
            }

            if (!int.TryParse(parts[0], out int inquiryId)) return line;
            if (!updates.TryGetValue(inquiryId, out var update)) return line;

            // 데이터 업데이트
            parts[3] = update.Status.ToString().ToUpper();
            parts[4] = update.ResponseMessage; // 이스케이프는 아래에서 일괄 처리

            updatedCount++;

            // [수정] 데이터 재조립 시 모든 필드에 대해 이스케이프 처리 재적용
            // 이렇게 해야 "Hello, World" 같은 데이터가 깨지지 않고 다시 "Hello, World"로 저장됩니다.
            var escapedParts = parts.Select(EscapeCsvField);

            return string.Join(",", escapedParts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CsvService] 라인 업데이트 오류: {ex.Message}");
            return line;
        }
    }

    /// <summary>
    /// CSV 라인을 쉼표로 분리 (따옴표 처리 포함)
    /// </summary>
    public List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // CSV 표준: 따옴표 안의 따옴표("")는 하나의 따옴표(")로 처리
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // 다음 따옴표 건너뛰기
                }
                else
                {
                    inQuotes = !inQuotes;
                }
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
        return result;
    }

    /// <summary>
    /// CSV 필드를 이스케이프 처리 (쉼표나 따옴표가 포함된 경우 따옴표로 감싸기)
    /// </summary>
    public string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        // 쉼표, 따옴표, 개행문자가 포함된 경우 따옴표로 감싸고 내부 따옴표는 이스케이프
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    #endregion
}

