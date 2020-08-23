using Microsoft.Extensions.Configuration;

namespace AWSLambdaRssNotification.Helper
{
    public interface IConfigurationService
    {
        IConfiguration GetConfiguration();
    }
}
