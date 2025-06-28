using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
    /// <summary>
    /// Servis za slanje obaveštenja konobarima o spremnim porudžbinama.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Obaveštava konobara da je porudžbina za dati sto spremna.
        /// </summary>
        /// <param name="tableId">ID stola za koji je porudžbina.</param>
        /// <param name="waiterId">ID konobara koji treba da primi obaveštenje.</param>
        void NotifyOrderReady(int tableId, int waiterId);
    }
}

