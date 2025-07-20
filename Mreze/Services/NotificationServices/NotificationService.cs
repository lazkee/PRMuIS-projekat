using System.Text;
using Domain.Repositories;
using Domain.Services;

namespace Services.NotificationServices
{
    public class NotificationService : INotificationService
    {
        private readonly IClientDirectory _directory;

        public NotificationService(IClientDirectory directory)
        {
            _directory = directory;
        }

        public void NotifyOrderReady(int tableId, int waiterId)
        {
            var waiter = _directory.GetById(waiterId);
            if (waiter != null)
            {
                var msg = $"READY;{tableId};{waiterId}\n";
                var data = Encoding.UTF8.GetBytes(msg);
                waiter.Socket.Send(data);
            }
        }
    }
}
