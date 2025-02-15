using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using ByteBox.Lambdas.Constants;
using ByteBox.Lambdas.Messages;
using NexaWrap.SQS.NET.Interfaces;

namespace ByteBox.Lambdas.Services;

public abstract class AThumbnailGenerator : IThumbnailGenerator
{
    protected readonly IAmazonS3 _s3Client;
    protected readonly IMessageSender _messageSender;

    protected AThumbnailGenerator(IAmazonS3 s3Client, IMessageSender messageSender)
    {
        _s3Client = s3Client;
        _messageSender = messageSender;
    }

    public abstract Task GenerateAsync(string bucketName, string fileKey, ILambdaContext context);

    protected async Task PublishThumbnailGeneratedMessageAsync(string bucketName, string thumbnailKey, Guid fileId, ILambdaContext context)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable(ConfigKeys.PreSignedExpiryInDays), out var expiresInDays))
        {
            context.Logger.LogError($"Failed to parse environment variable {ConfigKeys.PreSignedExpiryInDays}");
        }

        var preSignedUrlRequest = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = thumbnailKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddDays(expiresInDays)
        };
        var thumbnailPreSignedUrl = await _s3Client.GetPreSignedURLAsync(preSignedUrlRequest);

        var thumbnailGeneratedMessage = new ThumbnailGeneratedMessage
        {
            FileId = fileId,
            ThumbnailKey = thumbnailKey,
            ThumbnailPresignedUrl = thumbnailPreSignedUrl,
            CorrelationId = Guid.NewGuid().ToString()
        };
        await _messageSender.SendMessageAsync("ByteBox-FileStore-Dev", thumbnailGeneratedMessage);
    }
}
