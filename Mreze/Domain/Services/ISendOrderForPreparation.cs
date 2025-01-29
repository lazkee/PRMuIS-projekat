using System.Collections.Generic;
using Domain.Models;

namespace Domain.Services
{
    public interface ISendOrderForPreparation
    {
        void SendOrder(int WaiterId, List<Order> orders);
    }
}
