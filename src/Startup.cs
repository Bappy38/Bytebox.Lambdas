using Amazon.Lambda.Annotations;
using Amazon.S3;
using ByteBox.Lambdas.Constants;
using ByteBox.Lambdas.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexaWrap.SQS.NET.Extensions;

namespace ByteBox.Lambdas;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.ConfigureSqs(configuration, builder => { });

        services.AddAWSService<IAmazonS3>();

        services.AddKeyedScoped<IThumbnailGenerator, ImageThumbnailGenerator>(ServiceKeys.ImageThumbnailGenerator);
        services.AddKeyedScoped<IThumbnailGenerator, PdfThumbnailGenerator>(ServiceKeys.PdfThumbnailGenerator);
    }
}
