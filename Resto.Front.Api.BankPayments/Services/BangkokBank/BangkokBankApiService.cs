using Resto.Front.Api.BankPayments.Interfaces;
using Resto.Front.Api.BankPayments.Interfaces.Services.BangkokBank;
using System;
using System.Net.Http;

namespace Resto.Front.Api.BankPayments.Services.BangkokBank
{
    public class BangkokBankApiService : IBangkokBankApiService
    {
        private readonly ISettings settings;
        public BangkokBankApiService(ISettings settings) 
        { 
        }

        private HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            Uri baseUri = new Uri(settings.Kasikorn.AddressApi);
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;
            return client;
        }
        public void Dispose()
        {
        }
    }
}
