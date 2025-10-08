using System.ComponentModel.DataAnnotations;
using OrderNotifications.Models;

namespace OrderNotifications
{
    public class NotificationService : INotificationService
    {
        public void NotifyOrderStatus(Order order)
        {
            string message = "";
            string EmailMessage = "";
            string SMSMessage = "";
            string WAMessage = "";
            
            EmailMessage = "Enviando mensaje via Email a: " + order.Customer.ContactInfo.Email + ". Tu orden: " + order.Id + "con estatus: " + order.Status + " ha sido enviada.";
            
            if (order.Customer.CountryCode != "MX")
            {
                SMSMessage += "\n Enviando mensaje via SMS a: " + order.Customer.ContactInfo.PhoneNumber + ". Tu orden: " +
                                       order.Id + "con estatus: " + order.Status + " ha sido enviada";
            }

            if (order.Customer.CountryCode == "MX")
            {
                WAMessage += "\n Enviando mensaje via Whatsapp a: " + order.Customer.ContactInfo.PhoneNumber + ". Tu orden: " +
                                       order.Id + "con estatus: " + order.Status + " ha sido enviada";
            }

            if (order.Customer.CountryCode == "AK")
            {
                message = "Notificaciones deshabilitadas";
                EmailMessage = "";
                SMSMessage = "";
                WAMessage = "";
                
            }
            
            //enviar wa a KA aunque deshabilite
            foreach (KeyValuePair<NotificationChannel, NotificationChannelPreference> entry in order.Customer.Preferences)
            {
                if (entry.Value == NotificationChannelPreference.Disabled)
                {
                    switch (entry.Key)
                    {
                        case NotificationChannel.Email:
                            EmailMessage = "";
                            break;
                        case NotificationChannel.SMS:
                            SMSMessage = "";
                            break;
                        case NotificationChannel.WhatsApp:
                            WAMessage = "";
                            break;
                    }
                }
                else
                {
                    switch (entry.Key)
                    {
                        case NotificationChannel.Email:
                            EmailMessage = "Enviando mensaje via Email a: " + order.Customer.ContactInfo.Email + ". Tu orden: " + order.Id + "con estatus: " + order.Status + " ha sido enviada.";
                            break;
                        case NotificationChannel.SMS:
                            SMSMessage += "\n Enviando mensaje via SMS a: " + order.Customer.ContactInfo.PhoneNumber + ". Tu orden: " +
                                          order.Id + "con estatus: " + order.Status + " ha sido enviada";
                            break;
                        case NotificationChannel.WhatsApp:
                            if (order.Customer.CountryCode == "AK")
                            {
                                message = "Notificaciones deshabilitadas";
                            }
                            
                            WAMessage += "\n Enviando mensaje via Whatsapp a: " + order.Customer.ContactInfo.PhoneNumber + ". Tu orden: " +
                                         order.Id + "con estatus: " + order.Status + " ha sido enviada";
                            break;
                    }
                }
            }
            
            message += EmailMessage + SMSMessage + WAMessage;
            Console.WriteLine(message);
        }
    }
}
