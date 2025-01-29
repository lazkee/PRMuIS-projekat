using System.Collections.Generic;

namespace Domain.Repositories.WaiterRepository
{
    public interface IWaiterRepository
    {

        void SetWaiterState(int waiterId, bool isBusy);

        bool GetWaiterState(int waiterId);

        Dictionary<int, bool> GetAllWaiterStates();

    }
}
