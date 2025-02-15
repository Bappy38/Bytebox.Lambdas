using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using ByteBox.Lambdas.Constants;
using ImageMagick;
using NexaWrap.SQS.NET.Interfaces;

namespace ByteBox.Lambdas.Services;

public class PdfThumbnailGenerator : AThumbnailGenerator
{
    public PdfThumbnailGenerator(IAmazonS3 s3Client, IMessageSender messageSender) : base(s3Client, messageSender)
    {
    }

    public override async Task GenerateAsync(string bucketName, string fileKey, ILambdaContext context)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable(ConfigKeys.ThumbnailHeight), out var maxHeight))
        {
            context.Logger.LogError($"Failed to parse environment variable {ConfigKeys.ThumbnailHeight}");
        }
        if (!int.TryParse(Environment.GetEnvironmentVariable(ConfigKeys.ThumbnailWidth), out var maxWidth))
        {
            context.Logger.LogError($"Failed to parse environment variable {ConfigKeys.ThumbnailWidth}");
        }

        var response = await _s3Client.GetObjectAsync(bucketName, fileKey);

        var fileId = response.Metadata["x-amz-meta-file-id"];
        var thumbnailKey = fileKey.Replace("resources", "thumbnails");

        using var pdfStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(pdfStream);
        pdfStream.Position = 0;

        var readSettings = new MagickReadSettings()
        {
            Density = new Density(150)
        };

        using (var images = new MagickImageCollection())
        {
            images.Read(pdfStream, readSettings);

            using (var firstPage = images[0])
            {
                firstPage.Resize(new MagickGeometry((uint)maxWidth, (uint)maxHeight) { IgnoreAspectRatio = false });

                firstPage.Format = MagickFormat.Jpeg;

                using var thumbnailStream = new MemoryStream();
                firstPage.Write(thumbnailStream);
                thumbnailStream.Position = 0;

                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = thumbnailKey,
                    InputStream = thumbnailStream,
                    ContentType = "image/jpeg"
                };

                await _s3Client.PutObjectAsync(putRequest);
                context.Logger.LogLine($"Uploaded thumbnail to {thumbnailKey}");
            }
            await PublishThumbnailGeneratedMessageAsync(bucketName, thumbnailKey, Guid.Parse(fileId), context);
        }
    }
}
