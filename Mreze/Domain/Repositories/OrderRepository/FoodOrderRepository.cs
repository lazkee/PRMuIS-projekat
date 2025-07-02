using System.Collections.Concurrent;
using System.Collections.Generic;
using Domain.Models;

namespace Domain.Repositories.OrderRepository
{
    public class FoodOrderRepository : IOrderRepository
    {

        private static readonly BlockingCollection<List<Order>> _foodOrders = new BlockingCollection<List<Order>>();
        public void AddOrder(List<Order> order)
        {
            _foodOrders.Add(order);
        }

        public BlockingCollection<List<Order>> GetAllOrders()
        {
            return _foodOrders;
        }

        public List<Order> RemoveOrder()
        {

            return _foodOrders.Take(); // Uklanja sa početka reda
        }
    }
}
