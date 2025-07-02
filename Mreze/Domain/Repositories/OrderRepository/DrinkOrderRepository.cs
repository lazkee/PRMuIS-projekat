using Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories.OrderRepository
{
    public class DrinkOrderRepository : IOrderRepository
    {
        private static readonly BlockingCollection<List<Order>> _drinkOrders = new BlockingCollection<List<Order>>();
        public BlockingCollection<List<Order>> GetAllOrders()
        {
            return _drinkOrders;
        }

        public void AddOrder(List<Order> order)
        { 
            _drinkOrders.Add(order);
        }

        public List<Order> RemoveOrder()
        {
            
            return _drinkOrders.Take(); // Uklanja sa početka reda
        }
    }
}
