using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using ByteBox.Lambdas.Constants;
using NexaWrap.SQS.NET.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ByteBox.Lambdas.Services;

public class ImageThumbnailGeneratorService : AThumbnailGenerator
{
    public ImageThumbnailGeneratorService(IAmazonS3 s3Client, IMessageSender messageSender) : base(s3Client, messageSender)
    {
    }

    public override async Task GenerateAsync(string bucketName, string fileKey, ILambdaContext context)
    {
        var response = await _s3Client.GetObjectAsync(bucketName, fileKey);

        var fileId = response.Metadata["x-amz-meta-file-id"];
        var thumbnailKey = fileKey.Replace("resources", "thumbnails");

        if (!int.TryParse(Environment.GetEnvironmentVariable(ConfigKeys.ThumbnailHeight), out var maxHeight))
        {
            context.Logger.LogError($"Failed to parse environment variable {ConfigKeys.ThumbnailHeight}");
        }
        if (!int.TryParse(Environment.GetEnvironmentVariable(ConfigKeys.ThumbnailWidth), out var maxWidth))
        {
            context.Logger.LogError($"Failed to parse environment variable {ConfigKeys.ThumbnailWidth}");
        }

        using (var image = Image.Load(response.ResponseStream))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

            using (var stream = new MemoryStream())
            {
                image.Save(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                context.Logger.LogLine($"Image thumbnail size of {fileKey}: {stream.Length}");

                var uploadRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = thumbnailKey,
                    InputStream = stream
                };
                await _s3Client.PutObjectAsync(uploadRequest);
                context.Logger.LogLine($"Uploaded thumbnail to {thumbnailKey}");
            }
        }
        await PublishThumbnailGeneratedMessageAsync(bucketName, thumbnailKey, Guid.Parse(fileId), context);
    }
}
