using System.Collections.Concurrent;
using System.Collections.Generic;
using Domain.Models;

namespace Domain.Repositories.OrderRepository
{
    public interface IOrderRepository
    {
        BlockingCollection<List<Order>> GetAllOrders();
        void AddOrder(List<Order> order);
        List<Order> RemoveOrder();

    }
}
