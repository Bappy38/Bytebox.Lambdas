using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using ByteBox.Lambdas.Constants;
using ByteBox.Lambdas.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ByteBox.Lambdas.Functions;

public class PdfThumbnailGenerator
{
    [LambdaFunction(ResourceName = "PdfThumbnailGenerator", MemorySize = 256, Timeout = 120)]
    public async Task GeneratePdfThumbnail(S3Event evnt, ILambdaContext context, [FromKeyedServices(ServiceKeys.PdfThumbnailGenerator)] IThumbnailGenerator _thumbnailGenerator)
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
                context.Logger.LogLine($"Thumbnail generated successfully for PDF with FileKey: {fileKey}");
            }
            catch (Exception e)
            {
                context.Logger.LogError($"Error occurred while generating thumbnail for PDF with FileKey: {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}");
                context.Logger.LogError(e.Message);
                context.Logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
}
