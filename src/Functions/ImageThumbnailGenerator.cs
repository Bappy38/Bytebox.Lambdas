using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using ByteBox.Lambdas.Constants;
using ByteBox.Lambdas.Services;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ByteBox.Lambdas.Functions;

public class ImageThumbnailGenerator
{
    private readonly IThumbnailGenerator _thumbnailGenerator;

    public ImageThumbnailGenerator()
    {
    }

    public ImageThumbnailGenerator([FromKeyedServices(ServiceKeys.ImageThumbnailGenerator)] IThumbnailGenerator thumbnailGenerator)
    {
        _thumbnailGenerator = thumbnailGenerator;
    }

    [LambdaFunction(ResourceName = "ImageThumbnailGenerator", MemorySize = 256, Timeout = 120)]
    public async Task GenerateImageThumbnail(S3Event evnt, ILambdaContext context)
    {
        var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
        foreach (var record in eventRecords)
        {
            var s3Event = record.S3;
            if (s3Event == null)
            {
                continue;
            }

            try
            {
                var bucketName = s3Event.Bucket.Name;
                var fileKey = s3Event.Object.Key;
                await _thumbnailGenerator.GenerateAsync(bucketName, fileKey, context);
                context.Logger.LogLine($"Thumbnail generated successfully for image with FileKey: {fileKey}");
            }
            catch (Exception e)
            {
                context.Logger.LogError($"Error occurred while generating thumbnail for image with FileKey: {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}");
                context.Logger.LogError(e.Message);
                context.Logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
}
