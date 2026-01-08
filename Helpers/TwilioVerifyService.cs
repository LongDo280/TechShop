using Twilio;
using Twilio.Rest.Verify.V2.Service;
using Microsoft.Extensions.Configuration;
public class TwilioVerifyService
{
    private readonly IConfiguration _config;
    public TwilioVerifyService(IConfiguration config) => _config = config;

    // 1. Gửi mã OTP
    public async Task<string> SendVerificationCode(string phoneNumber)
    {
        TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);

        var verification = await VerificationResource.CreateAsync(
            to: phoneNumber,
            channel: "sms",
            pathServiceSid: _config["Twilio:VerificationServiceSid"]
        );
        return verification.Status;
    }

    // 2. Kiểm tra mã OTP khách nhập
    public async Task<bool> CheckVerificationCode(string phoneNumber, string code)
    {
        TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);

        var verificationCheck = await VerificationCheckResource.CreateAsync(
            to: phoneNumber,
            code: code,
            pathServiceSid: _config["Twilio:VerificationServiceSid"]
        );
        return verificationCheck.Status == "approved";
    }
}