using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageThumbnailGenerator;

public class Function
{
    IAmazonS3 S3Client { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client">The service client to access Amazon S3.</param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
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
                await GenerateImageThumbnailAsync(bucketName, fileKey, context);
            }
            catch (Exception e)
            {
                context.Logger.LogError($"Error occurred while generating thumbnail for object with key {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}");
                context.Logger.LogError(e.Message);
                context.Logger.LogError(e.StackTrace);
                throw;
            }
        }
    }

    private async Task GenerateImageThumbnailAsync(string bucketName, string fileKey, ILambdaContext context)
    {
        var response = await this.S3Client.GetObjectAsync(bucketName, fileKey);

        using (var image = Image.Load(response.ResponseStream))
        {
            var maxWidth = 500;     //TODO:: Read from configuration
            var maxHeight = 500;    //TODO:: Read from configuration

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

            using (var stream = new MemoryStream())
            {
                image.Save(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                context.Logger.LogLine($"Thumbnail size of {fileKey}: {stream.Length}");

                var thumbnailKey = fileKey.Replace("resources", "thumbnails");
                var uploadRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = thumbnailKey,
                    InputStream = stream
                };
                await this.S3Client.PutObjectAsync(uploadRequest);
                context.Logger.LogLine($"Uploaded thumbnail to {thumbnailKey}");
            }
        }
        // TODO:: Publish message to message broker so that FileStore can update the thumbnail location in db.
    }
}