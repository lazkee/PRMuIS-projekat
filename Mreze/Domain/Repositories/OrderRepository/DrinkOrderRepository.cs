using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories.OrderRepository
{
    public class DrinkOrderRepository : IOrderRepository
    {
        private static Queue<List<Order>> _drinkOrders = new Queue<List<Order>>();
        public Queue<List<Order>> GetAllOrders()
        {
            return _drinkOrders;
        }

        public void AddOrder(List<Order> order)
        { 
            _drinkOrders.Enqueue(order);
        }

        public List<Order> RemoveOrder()
        {
            return _drinkOrders.Dequeue();
        }
    }
}
