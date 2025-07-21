using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Domain.Repositories.WaiterRepository
{
    public class WaiterRepository : IWaiterRepository
    {
        // Thread‐safe skladišta stanja
        private static readonly ConcurrentDictionary<int, bool> _waiterBusy
            = new ConcurrentDictionary<int, bool>();
        private static readonly ConcurrentDictionary<int, bool> _orderReady
            = new ConcurrentDictionary<int, bool>();

        // Ovaj flag sprečava višestruku inicijalizaciju
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// numberOfWaiters očekuje maksimalni ID konobara (npr. 3 → IDs 1,2,3).
        /// </summary>
        public WaiterRepository(int numberOfWaiters)
        {
            if (numberOfWaiters < 1)
                throw new ArgumentException("Potrebno je bar 1 konobar.", nameof(numberOfWaiters));

            // Inicijalizujemo samo prvi put
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        for (int i = 1; i <= numberOfWaiters; i++)
                        {
                            _waiterBusy[i] = false;  // svi ispočetka FREE
                            _orderReady[i] = false;  // nijednom nije spremna
                        }
                        _initialized = true;
                    }
                }
                //Console.WriteLine($"Repo created in process {System.Diagnostics.Process.GetCurrentProcess().Id}");
            }
        }

        // --- IWaiterRepository: porudžbina spremna za nošenje ---
        public bool HasOrderReady(int waiterId)
        {
            ValidateId(waiterId);
            return _orderReady[waiterId];
        }

        public void SetOrderReady(int waiterId)
        {
            ValidateId(waiterId);
            _orderReady[waiterId] = true;
        }

        public void ClearOrderReady(int waiterId)
        {
            ValidateId(waiterId);
            _orderReady[waiterId] = false;
        }

        // --- Dodatne metode za stanje konobara (FREE/BUSY) ---
       
        public bool GetWaiterState(int waiterId)
        {
            ValidateId(waiterId);
            return _waiterBusy[waiterId];
        }

        
        public void SetWaiterState(int waiterId, bool isBusy)
        {
            ValidateId(waiterId);
            _waiterBusy[waiterId] = isBusy;
        }

        
        public Dictionary<int, bool> GetAllWaiterStates()
        {
            return new Dictionary<int, bool>(_waiterBusy);
        }

        private void ValidateId(int waiterId)
        {
            if (!_waiterBusy.ContainsKey(waiterId))
                throw new ArgumentOutOfRangeException(
                    nameof(waiterId),
                    $"ID konobara {waiterId} nije u opsegu 1..{_waiterBusy.Count}.");
        }
    }
}
