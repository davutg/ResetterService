using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ResetterService
{
    public partial class Helpers
    {
        public static string ApplicationExecutableName
        {
            get {
                return System.AppDomain.CurrentDomain.FriendlyName;
            }
        }

        public static string SendMail(string mailtoAddress, string mailSubject, string mailContent, string mailCCAddress = "", string mailBccAddress = "")
        {
            try
            {
                string smptpServer = ConfigurationManager.AppSettings["SmtpServer"];
                string smtpServerAuthenticateUser = ConfigurationManager.AppSettings["SmtpServerAuthenticateUser"];
                string smtpServerAuthenticatePassword = Helpers.Decrypt(ConfigurationManager.AppSettings["SmtpServerAuthenticatePassword"]);
                int smtpServerPort = Int32.Parse(ConfigurationManager.AppSettings["SmtpServerPort"]);
                string fromEmailAddress = ConfigurationManager.AppSettings["fromEmailAddress"];

                MailMessage mail = new MailMessage();
                mail.IsBodyHtml = true;
                mail.Body = mailContent;

                System.Net.Mime.ContentType contentType = new System.Net.Mime.ContentType("text/html");
                contentType.Name = mailSubject + ".html";

                //Geçici çözümdür
                if (mailContent.Contains("charset=utf-8"))
                {
                    mailContent = mailContent.Replace("charset=utf-8", "charset=iso-8859-9");
                }

                byte[] mailContentBytes = Encoding.Default.GetBytes(mailContent);
                Stream stream = new MemoryStream(mailContentBytes);
                Attachment mailAttach = new Attachment(stream, contentType);
                mail.Attachments.Add(mailAttach);
                //}

                mail.Subject = mailSubject;
                mail.Sender = new MailAddress(string.Format(fromEmailAddress, "{0} Uygulaması", ApplicationExecutableName));
                mail.From = mail.Sender;


                foreach (var toAddress in SplitMailList(mailtoAddress))
                {
                    mail.To.Add(new MailAddress(toAddress));
                }

                foreach (var ccAddress in SplitMailList(mailCCAddress))
                {
                    mail.CC.Add(new MailAddress(ccAddress));
                }


                foreach (var bccAddress in SplitMailList(mailBccAddress))
                {
                    mail.Bcc.Add(new MailAddress(bccAddress));
                }

                mail.Priority = MailPriority.High;
                mail.BodyEncoding = Encoding.UTF8;

                System.Net.Mail.SmtpClient smtpClient = new SmtpClient(smptpServer, smtpServerPort);
                smtpClient.EnableSsl = false;
                smtpClient.Credentials = new NetworkCredential(smtpServerAuthenticateUser, smtpServerAuthenticatePassword);

                smtpClient.Send(mail);
                return "ok";
            }
            catch (Exception e)
            {
                return e.Message;
            }

        }

        public static List<string> SplitMailList(string concatedAddresses)
        {
            if (!string.IsNullOrEmpty(concatedAddresses))
            {
                return concatedAddresses.Split(';').ToList();
            }
            else return new List<string>();
        }

    }
}
