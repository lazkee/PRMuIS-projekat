// Domain/Services/ISendOrderForPreparation.cs
using System.Collections.Generic;
using Domain.Models;

namespace Domain.Services
{
    public interface ISendOrderForPreparation
    {
        /// <summary>
        /// Enqueue-uje batch porudžbina za pripremu.
        /// </summary>
        /// <param name="waiterId">ID konobara.</param>
        /// <param name="tableNumber">Broj stola.</param>
        /// <param name="orders">Lista stavki.</param>
        void SendOrder(int waiterId, int tableNumber, List<Order> orders);
    }
}
