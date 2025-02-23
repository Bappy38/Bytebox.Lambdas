using NexaWrap.SQS.NET.Models;

namespace ByteBox.Lambdas.Messages;

public class ThumbnailGeneratedMessage : IMessage
{
    public Guid FileId { get; set; }
    public string ThumbnailKey { get; set; }
    public string ThumbnailPresignedUrl { get; set; }
    public DateTime ThumbnailPresignedGeneratedAt { get; set; }

    public string MessageTypeName => nameof(ThumbnailGeneratedMessage);
    public string? CorrelationId { get; set; }
}
