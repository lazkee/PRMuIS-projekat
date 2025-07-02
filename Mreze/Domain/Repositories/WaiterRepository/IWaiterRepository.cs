using System.Collections.Generic;

namespace Domain.Repositories.WaiterRepository
{
    /// <summary>
    /// Repozitorijum za praćenje stanja konobara i
    /// zastavice „porudžbina spremna za nošenje“ po ID-ju konobara.
    /// </summary>
    public interface IWaiterRepository
    {
        // -------- Stanje konobara (busy/free) --------

        /// <summary>
        /// Vraća true ako je konobar zauzet, false ako je slobodan.
        /// </summary>
        bool GetWaiterState(int waiterId);

        /// <summary>
        /// Postavlja stanje konobara: true = zauzet, false = slobodan.
        /// </summary>
        void SetWaiterState(int waiterId, bool isBusy);

        /// <summary>
        /// Vraća snapshot svih konobara i njihovih trenutnih stanja.
        /// </summary>
        Dictionary<int, bool> GetAllWaiterStates();

        // ----- Zastavica „porudžbina spremna“ (order ready) -----

        /// <summary>
        /// Vraća true ako je za datog konobara stigla porudžbina spremna za nošenje.
        /// </summary>
        bool HasOrderReady(int waiterId);

        /// <summary>
        /// Postavlja zastavicu da je porudžbina spremna za nošenje.
        /// </summary>
        void SetOrderReady(int waiterId);

        /// <summary>
        /// Resetuje zastavicu nakon što je porudžbina odnesena.
        /// </summary>
        void ClearOrderReady(int waiterId);
    }
}
