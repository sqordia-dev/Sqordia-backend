using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Sqordia.Application.Common.Interfaces;
using Sqordia.Domain.ValueObjects;
using System.Text.Json;

namespace Sqordia.Infrastructure.Services;

/// <summary>
/// Email service that sends emails via AWS SQS queue (processed by Lambda/SES)
/// </summary>
public class EmailService : IEmailService
{
    private readonly IAmazonSQS? _sqsClient;
    private readonly string? _emailQueueUrl;
    private readonly string _awsRegion;
    private readonly ILogger<EmailService> _logger;
    private readonly ILocalizationService _localizationService;

    public EmailService(
        IAmazonSQS? sqsClient,
        string? emailQueueUrl,
        string awsRegion,
        ILogger<EmailService> logger,
        ILocalizationService localizationService)
    {
        _sqsClient = sqsClient;
        _emailQueueUrl = emailQueueUrl;
        _awsRegion = awsRegion;
        _logger = logger;
        _localizationService = localizationService;
    }

    private async Task SendToQueueAsync(string emailType, string toEmail, string? toName, string subject, string body, string? htmlBody = null, Dictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(_emailQueueUrl) || _sqsClient == null)
        {
            _logger.LogWarning(
                "Email queue not configured. Email would be sent: Type={EmailType}, To={ToEmail}, Subject={Subject}",
                emailType, toEmail, subject);
            return;
        }

        try
        {
            var jobId = Guid.NewGuid().ToString();
            var message = new
            {
                jobId = jobId,
                emailType = emailType,
                toEmail = toEmail,
                toName = toName,
                subject = subject,
                body = body,
                htmlBody = htmlBody,
                metadata = metadata ?? new Dictionary<string, string>()
            };

            var messageBody = JsonSerializer.Serialize(message);
            var request = new SendMessageRequest
            {
                QueueUrl = _emailQueueUrl,
                MessageBody = messageBody
            };

            var response = await _sqsClient.SendMessageAsync(request);
            _logger.LogInformation(
                "Email job {JobId} queued successfully. Type={EmailType}, To={ToEmail}, MessageId={MessageId}",
                jobId, emailType, toEmail, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email job. Type={EmailType}, To={ToEmail}", emailType, toEmail);
            throw;
        }
    }

    public async Task SendEmailAsync(Domain.ValueObjects.EmailAddress to, string subject, string body)
    {
        await SendToQueueAsync("simple", to.Value, null, subject, body);
    }

    public async Task SendEmailAsync(IEnumerable<Domain.ValueObjects.EmailAddress> to, string subject, string body)
    {
        // Send individual emails for each recipient
        var tasks = to.Select(email => SendToQueueAsync("simple", email.Value, null, subject, body));
        await Task.WhenAll(tasks);
    }

    public async Task SendHtmlEmailAsync(Domain.ValueObjects.EmailAddress to, string subject, string htmlBody)
    {
        await SendToQueueAsync("html", to.Value, null, subject, string.Empty, htmlBody);
    }

    public async Task SendHtmlEmailAsync(IEnumerable<Domain.ValueObjects.EmailAddress> to, string subject, string htmlBody)
    {
        var tasks = to.Select(email => SendToQueueAsync("html", email.Value, null, subject, string.Empty, htmlBody));
        await Task.WhenAll(tasks);
    }

    public async Task SendWelcomeWithVerificationAsync(string email, string firstName, string lastName, string userName, string verificationToken)
    {
        var htmlBody = GetWelcomeWithVerificationTemplate(firstName, lastName, verificationToken);
        var metadata = new Dictionary<string, string>
        {
            { "firstName", firstName },
            { "lastName", lastName },
            { "userName", userName },
            { "verificationToken", verificationToken }
        };
        await SendToQueueAsync("welcome_verification", email, $"{firstName} {lastName}", "Welcome to Sqordia - Verify Your Email", string.Empty, htmlBody, metadata);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName, string lastName)
    {
        var subject = _localizationService.GetString("Email.Subject.Welcome");
        var htmlBody = GetWelcomeEmailTemplate(firstName, lastName);
        var metadata = new Dictionary<string, string>
        {
            { "firstName", firstName },
            { "lastName", lastName }
        };
        await SendToQueueAsync("welcome", email, $"{firstName} {lastName}", subject, string.Empty, htmlBody, metadata);
    }

    public async Task SendEmailVerificationAsync(string email, string userName, string verificationToken)
    {
        var subject = _localizationService.GetString("Email.Subject.Verification");
        var htmlBody = GetEmailVerificationTemplate(userName, verificationToken);
        var metadata = new Dictionary<string, string>
        {
            { "userName", userName },
            { "verificationToken", verificationToken }
        };
        await SendToQueueAsync("verification", email, userName, subject, string.Empty, htmlBody, metadata);
    }

    public async Task SendPasswordResetAsync(string email, string userName, string resetToken)
    {
        var subject = _localizationService.GetString("Email.Subject.PasswordReset");
        var htmlBody = GetPasswordResetTemplate(userName, resetToken);
        var metadata = new Dictionary<string, string>
        {
            { "userName", userName },
            { "resetToken", resetToken }
        };
        await SendToQueueAsync("password_reset", email, userName, subject, string.Empty, htmlBody, metadata);
    }

    public async Task SendAccountLockedAsync(string email, string userName, DateTime lockedUntil)
    {
        var subject = _localizationService.GetString("Email.Subject.AccountLocked");
        var htmlBody = GetAccountLockedTemplate(userName, lockedUntil);
        var metadata = new Dictionary<string, string>
        {
            { "userName", userName },
            { "lockedUntil", lockedUntil.ToString("O") }
        };
        await SendToQueueAsync("account_locked", email, userName, subject, string.Empty, htmlBody, metadata);
    }

    public async Task SendLoginAlertAsync(string email, string userName, string ipAddress, DateTime loginTime)
    {
        var subject = _localizationService.GetString("Email.Subject.LoginAlert");
        var htmlBody = GetLoginAlertTemplate(userName, ipAddress, loginTime);
        var metadata = new Dictionary<string, string>
        {
            { "userName", userName },
            { "ipAddress", ipAddress },
            { "loginTime", loginTime.ToString("O") }
        };
        await SendToQueueAsync("login_alert", email, userName, subject, string.Empty, htmlBody, metadata);
    }

    public async Task SendAccountLockoutNotificationAsync(string email, string firstName, TimeSpan lockoutDuration, DateTime lockedAt)
    {
        var subject = _localizationService.GetString("Email.Subject.AccountLocked");
        var htmlBody = GetAccountLockoutTemplate(firstName, lockoutDuration, lockedAt);
        var metadata = new Dictionary<string, string>
        {
            { "firstName", firstName },
            { "lockoutDuration", lockoutDuration.TotalMinutes.ToString() },
            { "lockedAt", lockedAt.ToString("O") }
        };
        await SendToQueueAsync("account_lockout", email, firstName, subject, string.Empty, htmlBody, metadata);
    }

    public async Task SendOrganizationInvitationAsync(string email, string invitationToken, string? message = null)
    {
        var subject = _localizationService.GetString("Email.Subject.OrganizationInvitation");
        var htmlBody = GetOrganizationInvitationTemplate(email, invitationToken, message);
        var metadata = new Dictionary<string, string>
        {
            { "invitationToken", invitationToken }
        };
        if (!string.IsNullOrEmpty(message))
        {
            metadata["message"] = message;
        }
        await SendToQueueAsync("organization_invitation", email, email, subject, string.Empty, htmlBody, metadata);
    }

    // Template methods remain the same - they generate HTML content
    private string GetEmailVerificationTemplate(string userName, string verificationToken)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #3b82f6; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
        </div>
        <h2>{_localizationService.GetString("Email.Verification.Title")}</h2>
        <p>{_localizationService.GetString("Email.Verification.Greeting", userName)}</p>
        <p>{_localizationService.GetString("Email.Verification.ThankYou")}</p>
        <p style='text-align: center; margin: 30px 0;'>
            <a href='https://localhost:7001/verify-email?token={verificationToken}' class='button'>{_localizationService.GetString("Email.Verification.ButtonText")}</a>
        </p>
        <p>{_localizationService.GetString("Email.Verification.AlternativeText")}</p>
        <p style='word-break: break-all; color: #666;'>https://localhost:7001/verify-email?token={verificationToken}</p>
        <p>{_localizationService.GetString("Email.Verification.ExpiryNote")}</p>
        <div class='footer'>
            <p>{_localizationService.GetString("Email.Verification.IgnoreNote")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetPasswordResetTemplate(string userName, string resetToken)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #ef4444; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
        </div>
        <h2>{_localizationService.GetString("Email.PasswordReset.Title")}</h2>
        <p>{_localizationService.GetString("Email.PasswordReset.Greeting", userName)}</p>
        <p>{_localizationService.GetString("Email.PasswordReset.RequestReceived")}</p>
        <p style='text-align: center; margin: 30px 0;'>
            <a href='https://localhost:7001/reset-password?token={resetToken}' class='button'>{_localizationService.GetString("Email.PasswordReset.ButtonText")}</a>
        </p>
        <p>{_localizationService.GetString("Email.PasswordReset.AlternativeText")}</p>
        <p style='word-break: break-all; color: #666;'>https://localhost:7001/reset-password?token={resetToken}</p>
        <p>{_localizationService.GetString("Email.PasswordReset.ExpiryNote")}</p>
        <div class='footer'>
            <p>{_localizationService.GetString("Email.PasswordReset.IgnoreNote")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetAccountLockedTemplate(string userName, DateTime lockedUntil)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .alert {{ background-color: #fef2f2; border: 1px solid #fecaca; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
        </div>
        <h2>{_localizationService.GetString("Email.AccountLocked.Title")}</h2>
        <p>{_localizationService.GetString("Email.AccountLocked.Greeting", userName)}</p>
        <div class='alert'>
            <p><strong>{_localizationService.GetString("Email.AccountLocked.Notification")}</strong></p>
            <p>{_localizationService.GetString("Email.LoginAlert.Time", lockedUntil.ToString("yyyy-MM-dd HH:mm"))} UTC</p>
        </div>
        <p>{_localizationService.GetString("Email.AccountLocked.AutoUnlock")}</p>
        <p>{_localizationService.GetString("Email.AccountLocked.ContactSupport")}</p>
        <div class='footer'>
            <p>{_localizationService.GetString("Email.AccountLocked.ContactSupport")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetLoginAlertTemplate(string userName, string ipAddress, DateTime loginTime)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .info {{ background-color: #f0f9ff; border: 1px solid #0ea5e9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
        </div>
        <h2>{_localizationService.GetString("Email.LoginAlert.Title")}</h2>
        <p>{_localizationService.GetString("Email.LoginAlert.Greeting", userName)}</p>
        <p>{_localizationService.GetString("Email.LoginAlert.Notification")}</p>
        <div class='info'>
            <p><strong>{_localizationService.GetString("Email.LoginAlert.Time", loginTime.ToString("yyyy-MM-dd HH:mm"))}</strong> UTC</p>
            <p><strong>{_localizationService.GetString("Email.LoginAlert.IpAddress", ipAddress)}</strong></p>
        </div>
        <p><strong>{_localizationService.GetString("Email.LoginAlert.WasYou")}</strong> {_localizationService.GetString("Email.LoginAlert.NoAction")}</p>
        <p><strong>{_localizationService.GetString("Email.LoginAlert.NotYou")}</strong> {_localizationService.GetString("Email.LoginAlert.TakeAction")}</p>
        <div class='footer'>
            <p>{_localizationService.GetString("Email.AccountLocked.ContactSupport")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetWelcomeWithVerificationTemplate(string firstName, string lastName, string verificationToken)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 28px; font-weight: bold; color: #3b82f6; margin-bottom: 10px; }}
        .welcome {{ font-size: 24px; color: #059669; font-weight: bold; margin-bottom: 20px; }}
        .highlight {{ background-color: #fff7ed; border-left: 4px solid #f59e0b; padding: 20px; margin: 25px 0; border-radius: 5px; }}
        .verify-section {{ background-color: #f0f9ff; border: 2px solid #3b82f6; padding: 25px; margin: 25px 0; border-radius: 8px; text-align: center; }}
        .button {{ display: inline-block; padding: 14px 35px; background-color: #3b82f6; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
            <div class='welcome'>{_localizationService.GetString("Email.Welcome.Greeting", firstName)}</div>
        </div>
        
        <p>{_localizationService.GetString("Email.Welcome.ThankYou")}</p>
        
        <div class='highlight'>
            <p><strong>{_localizationService.GetString("Email.Welcome.NextStep")}</strong> {_localizationService.GetString("Email.Welcome.VerificationInstruction")}</p>
        </div>
        
        <div class='verify-section'>
            <h3 style='margin-top: 0; color: #1e40af;'>{_localizationService.GetString("Email.Verification.Title")}</h3>
            <p style='margin: 15px 0;'>{_localizationService.GetString("Email.Verification.ThankYou")}</p>
            <p style='margin: 25px 0;'>
                <a href='https://localhost:7001/verify-email?token={verificationToken}' class='button'>{_localizationService.GetString("Email.Verification.ButtonText")}</a>
            </p>
            <p style='font-size: 12px; color: #666; margin-top: 20px;'>{_localizationService.GetString("Email.Verification.ExpiryNote")}</p>
        </div>
        
        <p style='font-size: 13px; color: #666;'>{_localizationService.GetString("Email.Verification.AlternativeText")}</p>
        <p style='word-break: break-all; font-size: 12px; color: #3b82f6; background-color: #f8fafc; padding: 10px; border-radius: 4px;'>https://localhost:7001/verify-email?token={verificationToken}</p>
        
        <div class='footer'>
            <p>{_localizationService.GetString("Email.Verification.IgnoreNote")}</p>
            <p>{_localizationService.GetString("Email.Welcome.Support")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetWelcomeEmailTemplate(string firstName, string lastName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 28px; font-weight: bold; color: #3b82f6; margin-bottom: 10px; }}
        .welcome {{ font-size: 24px; color: #059669; font-weight: bold; margin-bottom: 20px; }}
        .highlight {{ background-color: #f0f9ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; }}
        .features {{ margin: 30px 0; }}
        .feature {{ display: flex; align-items: center; margin: 15px 0; }}
        .feature-icon {{ width: 24px; height: 24px; background-color: #3b82f6; border-radius: 50%; margin-right: 15px; display: flex; align-items: center; justify-content: center; color: white; font-weight: bold; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
        .cta {{ text-align: center; margin: 30px 0; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #3b82f6; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
            <div class='welcome'>🎉 {_localizationService.GetString("Email.Welcome.Greeting", firstName)}</div>
        </div>
        
        <p>{_localizationService.GetString("Email.Welcome.ThankYou")}</p>
        
        <div class='highlight'>
            <p><strong>{_localizationService.GetString("Email.Welcome.NextStep")}</strong> {_localizationService.GetString("Email.Welcome.VerificationInstruction")}</p>
        </div>
        
        <h3>{_localizationService.GetString("Email.Welcome.WhatYouCanDo")}</h3>
        <div class='features'>
            <div class='feature'>
                <div class='feature-icon'>📊</div>
                <span>{_localizationService.GetString("Email.Welcome.Feature1")}</span>
            </div>
            <div class='feature'>
                <div class='feature-icon'>📈</div>
                <span>{_localizationService.GetString("Email.Welcome.Feature2")}</span>
            </div>
            <div class='feature'>
                <div class='feature-icon'>🤝</div>
                <span>{_localizationService.GetString("Email.Welcome.Feature3")}</span>
            </div>
            <div class='feature'>
                <div class='feature-icon'>📋</div>
                <span>{_localizationService.GetString("Email.Welcome.Feature4")}</span>
            </div>
        </div>
        
        <div class='cta'>
            <p>{_localizationService.GetString("Email.Welcome.ReadyToStart")}</p>
        </div>
        
        <div class='footer'>
            <p>{_localizationService.GetString("Email.Welcome.Support")}</p>
            <p>{_localizationService.GetString("Email.Welcome.HereToHelp")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetOrganizationInvitationTemplate(string email, string invitationToken, string? message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #3b82f6; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
        .message {{ background-color: #f0f9ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
        </div>
        <h2>Organization Invitation</h2>
        <p>Hi,</p>
        <p>You have been invited to join an organization on Sqordia! This invitation will allow you to collaborate on business plans and access shared resources.</p>
        
        {(string.IsNullOrEmpty(message) ? "" : $@"
        <div class='message'>
            <p><strong>Personal Message:</strong></p>
            <p>{message}</p>
        </div>")}
        
        <p style='text-align: center; margin: 30px 0;'>
            <a href='https://localhost:7001/accept-invitation?token={invitationToken}' class='button'>Accept Invitation</a>
        </p>
        
        <p>If you're unable to click the button, copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #666;'>https://localhost:7001/accept-invitation?token={invitationToken}</p>
        
        <p>This invitation will expire in 7 days.</p>
        
        <div class='footer'>
            <p>If you didn't expect this invitation, you can safely ignore this email.</p>
            <p>&copy; 2024 Sqordia. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetAccountLockoutTemplate(string firstName, TimeSpan lockoutDuration, DateTime lockedAt)
    {
        var unlockTime = lockedAt.Add(lockoutDuration);
        var minutesRemaining = (int)lockoutDuration.TotalMinutes;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; background-color: #ef4444; color: white; padding: 20px; border-radius: 8px; }}
        .logo {{ font-size: 28px; font-weight: bold; margin-bottom: 10px; }}
        .alert {{ font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
        .warning-box {{ background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 20px; margin: 25px 0; border-radius: 5px; }}
        .info-box {{ background-color: #f0f9ff; border-left: 4px solid: #3b82f6; padding: 20px; margin: 25px 0; border-radius: 5px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
        .time {{ font-size: 18px; font-weight: bold; color: #ef4444; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>Sqordia</div>
            <div class='alert'>{_localizationService.GetString("Email.AccountLocked.Title")}</div>
        </div>
        
        <p>{_localizationService.GetString("Email.AccountLocked.Greeting", firstName)}</p>
        
        <div class='warning-box'>
            <p><strong>{_localizationService.GetString("Email.AccountLocked.Title")}</strong></p>
            <p>{_localizationService.GetString("Email.AccountLocked.Notification")}</p>
        </div>
        
        <div class='info-box'>
            <p><strong>{_localizationService.GetString("Email.LoginAlert.Details")}</strong></p>
            <p>{_localizationService.GetString("Email.LoginAlert.Time", lockedAt.ToString("yyyy-MM-dd HH:mm:ss"))} UTC</p>
            <p>{_localizationService.GetString("Email.AccountLocked.Duration", minutesRemaining)}</p>
            <p>{_localizationService.GetString("Email.LoginAlert.Time", unlockTime.ToString("yyyy-MM-dd HH:mm:ss"))} UTC</p>
        </div>
        
        <p><strong>{_localizationService.GetString("Email.AccountLocked.NotYou")}</strong></p>
        <ul>
            <li>{_localizationService.GetString("Email.AccountLocked.AutoUnlock")}</li>
            <li>{_localizationService.GetString("Email.AccountLocked.SecurityAdvice")}</li>
            <li>{_localizationService.GetString("Email.AccountLocked.ContactSupport")}</li>
        </ul>
        
        <p><strong>{_localizationService.GetString("Email.AccountLocked.NotYou")}</strong></p>
        <p>{_localizationService.GetString("Email.AccountLocked.ContactSupport")}</p>
        
        <div class='footer'>
            <p>{_localizationService.GetString("Email.Footer.DoNotReply")}</p>
            <p>{_localizationService.GetString("Email.AccountLocked.ContactSupport")}</p>
            <p>{_localizationService.GetString("Email.Footer.Copyright")}</p>
        </div>
    </div>
</body>
</html>";
    }
}
