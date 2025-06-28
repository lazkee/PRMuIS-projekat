using Domain.Repositories;
using Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var msg = $"Porudzbina za sto {tableId} je gotova\n";
                var data = Encoding.UTF8.GetBytes(msg);
                waiter.Socket.GetStream().Write(data, 0, data.Length);
            }
        }
    }
}
