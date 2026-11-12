namespace PdfAssistant.Application.Interfaces
{
    public interface IEmbeddingService
    {
        Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default);
    }
}
