namespace Domain.Services
{
    public interface IMakeAnOrder
    {
        void MakeAnOrder(int brojSlobodnogStola, int brojGostiju, int WaiterID);
    }
}
