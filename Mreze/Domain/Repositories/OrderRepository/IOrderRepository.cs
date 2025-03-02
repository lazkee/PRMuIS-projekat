using Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories.OrderRepository
{
    public interface IOrderRepository 
    {
        BlockingCollection<List<Order>> GetAllOrders();
        void AddOrder(List<Order> order);
        List<Order> RemoveOrder();
        
    }
}
