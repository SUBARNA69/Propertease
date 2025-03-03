using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;


namespace Propertease.Repos
{
 
        public class SmsService
        {
            private readonly IConfiguration _configuration;

            public SmsService(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public void SendSms(string toPhoneNumber, string message)
            {
                var accountSid = _configuration["TwilioSettings:AccountSid"];
                var authToken = _configuration["TwilioSettings:AuthToken"];
                var fromPhoneNumber = _configuration["TwilioSettings:FromPhoneNumber"];

                TwilioClient.Init(accountSid, authToken);

                var to = new PhoneNumber(toPhoneNumber);
                var from = new PhoneNumber(fromPhoneNumber);

                var smsMessage = MessageResource.Create(
                    to: to,
                    from: from,
                    body: message
                );

                Console.WriteLine($"SMS sent to {toPhoneNumber}: {smsMessage.Sid}");
            }
        }
}
