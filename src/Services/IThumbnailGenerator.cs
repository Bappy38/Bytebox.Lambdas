using Amazon.Lambda.Core;

namespace ByteBox.Lambdas.Services;

public interface IThumbnailGenerator
{
    Task GenerateAsync(string bucketName, string fileKey, ILambdaContext context);
}
