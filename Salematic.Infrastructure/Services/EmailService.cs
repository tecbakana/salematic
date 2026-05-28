using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Salematic.Domain.Interfaces;

namespace Salematic.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly bool _enableSsl;
        private readonly string _usuario;
        private readonly string _senha;
        private readonly string _remetente;
        private readonly string _nomeRemetente;

        public EmailService(IConfiguration config)
        {
            _host = config["Email:Smtp:Host"] ?? throw new InvalidOperationException("Email:Smtp:Host não configurado.");
            _port = int.TryParse(config["Email:Smtp:Port"], out var p) ? p : 587;
            _enableSsl = bool.TryParse(config["Email:Smtp:EnableSsl"], out var s) ? s : true;
            _usuario = config["Email:Smtp:Usuario"] ?? throw new InvalidOperationException("Email:Smtp:Usuario não configurado.");
            _senha = config["Email:Smtp:Senha"] ?? throw new InvalidOperationException("Email:Smtp:Senha não configurado.");
            _remetente = config["Email:Remetente"] ?? _usuario;
            _nomeRemetente = config["Email:NomeRemetente"] ?? "Salematic";
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml)
        {
            using var client = new SmtpClient(_host, _port)
            {
                Credentials = new NetworkCredential(_usuario, _senha),
                EnableSsl = _enableSsl
            };

            using var mensagem = new MailMessage
            {
                From = new MailAddress(_remetente, _nomeRemetente),
                Subject = assunto,
                Body = corpoHtml,
                IsBodyHtml = true
            };
            mensagem.To.Add(destinatario);

            await client.SendMailAsync(mensagem);
        }
    }
}
