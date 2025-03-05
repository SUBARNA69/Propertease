namespace Propertease.Repos
{
    using Amazon;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;

    namespace Propertease.Services
    {
        public class AwsSnsService
        {
            private readonly IConfiguration _configuration;

            public AwsSnsService(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public async Task SendSmsAsync(string phoneNumber, string message)
            {
                var awsAccessKey = _configuration["AwsSettings:AccessKey"];
                var awsSecretKey = _configuration["AwsSettings:SecretKey"];
                var region = _configuration["AwsSettings:Region"];

                var snsClient = new AmazonSimpleNotificationServiceClient(awsAccessKey, awsSecretKey, RegionEndpoint.GetBySystemName(region));

                var request = new PublishRequest
                {
                    PhoneNumber = phoneNumber,
                    Message = message
                };

                try
                {
                    var response = await snsClient.PublishAsync(request);
                    Console.WriteLine($"SMS sent: {response.MessageId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send SMS: {ex.Message}");
                }
            }
        }
    }
}
