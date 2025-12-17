using CategorizationAgent.DTOs;
using CategorizationAgent.Services;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace CategorizationAgent.Executors;

public class SimpleInquiryReadExecutor(string filePath, CsvService csvService)
    : ReflectingExecutor<SimpleInquiryReadExecutor>("SimpleInquiryReadExecutor"),
        IMessageHandler<string, List<Inquiry>>,
        IMessageHandler<List<ChatMessage>, List<Inquiry>>
{
    public async ValueTask<List<Inquiry>> HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[SimpleInquiryReadExecutor] 시작");
        return await csvService.ReadInquiriesAsync(filePath, cancellationToken);
    }

    public async ValueTask<List<Inquiry>> HandleAsync(
        List<ChatMessage> message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[SimpleInquiryReadExecutor] 시작");
        return await csvService.ReadInquiriesAsync(filePath, cancellationToken);
    }
}