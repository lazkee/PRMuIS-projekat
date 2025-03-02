using System;
using Domain.Services;

namespace Services.PrepareOrderServices
{
    public class PrepareFoodService : IPrepareOrder
    {
        public void Prepare()
        {
            throw new NotImplementedException();
        }
        //metoda za kuvara, ponasace se kao server za tcp(cekace da mu neko nesto posalje (server) i kad mu posalje onda ceka neko vreme (simulira izradu jela) i salje ga nazad serveru koji salje konobaru)
        //kad primi nesto onda postavi svoj busy na true
        //problem kako da listujemo sve barmene, ili posto su aplikacije svi, da samo stavimo da server pokusava da posalje jednom od njih sve dok ne bude mogao da posalje
        //isto ovako sve za barmene
    }
}
