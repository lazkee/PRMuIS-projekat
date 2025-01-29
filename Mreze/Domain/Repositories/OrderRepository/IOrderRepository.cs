using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories.OrderRepository
{
    public interface IOrderRepository 
    {
        Queue<List<Order>> GetAllOrders();
        void AddOrder(List<Order> order);
        List<Order> RemoveOrder();
        
    }
}
