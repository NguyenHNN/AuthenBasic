﻿using Microsoft.Extensions.Configuration;
using Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly string _fromPassword;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly bool _useSsl;

        public EmailService(IConfiguration configuration)
        {
            _fromEmail = configuration["EmailSettings:FromEmail"];
            _fromPassword = configuration["EmailSettings:FromPassword"];
            _smtpHost = configuration["EmailSettings:SmtpHost"];
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);
            _useSsl = bool.Parse(configuration["EmailSettings:UseSsl"]);
        }
        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.Credentials = new NetworkCredential(_fromEmail, _fromPassword);
                    client.EnableSsl = _useSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(to);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }
    }
}
