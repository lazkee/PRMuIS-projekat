using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories.OrderRepository
{
    public class FoodOrderRepository : IOrderRepository
    {
        private static Queue<List<Order>> _foodOrders = new Queue<List<Order>>();

        public void AddOrder(List<Order> order)
        {
            _foodOrders.Enqueue(order);
        }

        public Queue<List<Order>> GetAllOrders()
        {
            return _foodOrders ;
        }

        public List<Order> RemoveOrder()
        {
            return _foodOrders.Dequeue();
        }
    }
}
