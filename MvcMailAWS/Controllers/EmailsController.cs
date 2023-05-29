using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Net;
using System.Net.Mail;

namespace MvcMailAWS.Controllers
{
    public class EmailsController : Controller
    {

        private IConfiguration configuration;

        public EmailsController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index
            (string email, string subject, string body)
        {
            string user = this.configuration.GetValue<string>("AWS:EmailCredentials:User");
            string emailSender = this.configuration.GetValue<string>("AWS:EmailCredentials:Email");
            string server = this.configuration.GetValue<string>("AWS:EmailCredentials:Server");
            string password = this.configuration.GetValue<string>("AWS:EmailCredentials:Password");
            MailMessage message = new MailMessage();
            //From: cuenta del sender de aws
            message.From = new MailAddress(emailSender);
            message.To.Add(new MailAddress(email));
            message.Subject = subject;
            message.Body = body;
            //configuramos las credenciales de nuestro servicio
            NetworkCredential credentials = new NetworkCredential(user, password);
            //Configuramos el servidor
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = server;
            smtpClient.Port = 25;
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = true;
            smtpClient.Credentials = credentials;
            await smtpClient.SendMailAsync(message);
            ViewData["MENSAJE"] = "Mail enviado correctamente amigo :) ";
            return View();
        }

        public IActionResult MailAws()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> MailAWS
    (string email, string subject, string body
    , IFormFile file)
        {
            string emailSender =
                this.configuration.GetValue<string>
                ("AWS:EmailCredentials:Email");
            AmazonSimpleEmailServiceClient client =
                new AmazonSimpleEmailServiceClient(RegionEndpoint.USEast1);
            //PARA PODER ENVIAR ADJUNTOS, DEBEMOS MODIFICAR NUESTRO CODIGO
            if (file != null)
            {
                //PARA ENVIAR UN STREAM, NECESITAMOS PODER ENVIARLO
                //DENTRO DEL REQUEST.
                //PARA ELLO, VAMOS A CONFIGURAR TODO EL MESSAGE JUNTO 
                //A SUS FICHEROS ADJUNTOS Y DEBEMOS ALMACENARLO EN UN STREAM
                using (MemoryStream memory = new MemoryStream())
                {
                    //DEBEMOS ENVIAR UN OBJETO STREAM DENTRO DEL MAIL
                    MimeMessage message = new MimeMessage();
                    BodyBuilder builder = new BodyBuilder();
                    builder.TextBody = body;
                    message.From.Add(MailboxAddress.Parse(emailSender));
                    message.To.Add(MailboxAddress.Parse(email));
                    message.Subject = subject;
                    //DEBEMOS LEER EL FICHERO ADJUNTO Y ASOCIARLO A NUESTRO 
                    //BUILDER
                    using (Stream stream = file.OpenReadStream())
                    {
                        builder.Attachments.Add(file.FileName, stream);
                    }
                    //ASOCIAMOS EN EL MENSAJE EL STREAM
                    message.Body = builder.ToMessageBody();
                    //ALMACENAMOS EL MENSAJE EN MEMORIA STREAM
                    await message.WriteToAsync(memory);
                    //ENVIAMOS UTILIZANDO UNA PETICION RAW, QUE CONTIENE
                    //EN BYTES LOS ADJUNTOS
                    SendRawEmailRequest request = new SendRawEmailRequest();
                    //GUARDAMOS EN SU PROPIEDAD Data EL STREAM DE LA MEMORIA
                    //DEL MENSAJE
                    request.RawMessage = new RawMessage() { Data = memory };
                    await client.SendRawEmailAsync(request);
                    ViewData["MENSAJE"] = "Email con adjuntos enviado";
                    return View();
                }
            }
            else
            {
                Destination destination = new Destination();
                //CREAMOS UNA COLECCION DONDE ENVIAREMOS LOS MAILS
                destination.ToAddresses = new List<string> { email };
                Message message = new Message();
                message.Subject = new Amazon.SimpleEmail.Model.Content(subject);
                message.Body =
                    new Body(new Amazon.SimpleEmail.Model.Content(body));
                //TODOS LOS SERVICIOS AWS SON IGUALES.  TENDREMOS UN 
                //REQUEST Y UN RESPONSE
                SendEmailRequest request = new SendEmailRequest();
                //DEBEMOS INDICAR TRES DATOS 
                request.Destination = destination;
                request.Message = message;
                //NECESITA EL SOURCE, QUE ES EL EMAIL DE USER VERIFIED
                //DE NUESTRO SERVICIO
                request.Source = emailSender;
                //NECESITAMOS LAS CREDENCIALES DE APPSETTINGS???: NOOO
                SendEmailResponse response =
                    await client.SendEmailAsync(request);
                ViewData["MENSAJE"] = "Email enviado correctamente AWS";
                return View();
            }
        }

    }


}
