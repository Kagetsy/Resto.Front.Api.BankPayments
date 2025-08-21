using Newtonsoft.Json;
using Resto.Front.Api.BankPayments.Helpers;
using Resto.Front.Api.BankPayments.Interfaces;
using Resto.Front.Api.BankPayments.Interfaces.Services.KasikornBank;
using Resto.Front.Api.BankPayments.Models.Kasikorn.Requests;
using Resto.Front.Api.BankPayments.Models.Kasikorn.Responses;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Resto.Front.Api.BankPayments.Services.KasikornBank
{
    public class KasikornBankApiService : IKasikornBankApiService
    {
        private KasikornAuthResponse token;
        private KasikornAuthResponse Token
        {
            get
            {
                return token;
            }
            set
            {
                token = value;
                if (int.TryParse(token.expires_in, out expiresIn))
                    StartTokenExpiresTimer();
            }
        }
        private int expiresIn;
        private readonly ISettings settings;

        private System.Timers.Timer tokenExpiresTimer;
        private int tryCount = 0;
        private object locker = new object();
        public KasikornBankApiService(ISettings settings)
        {
            this.settings = settings;
        }

        private HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            Uri baseUri = new Uri(settings.Kasikorn.AddressApi);
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;
#if DEBUG
            client.DefaultRequestHeaders.Add("x-test-mode", "true");
#endif
            return client;
        }

        private async void Auth(CancellationToken cancellationToken)
        {
            try
            {
                PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(Auth)}] Start auth response");
                var authInfo = $"{settings.Kasikorn.Id}:{settings.Kasikorn.Secret}".ToBase64String();
                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
#if DEBUG
                    client.DefaultRequestHeaders.Add("env-id", "OAUTH2");
#endif
                    var user = new
                    {
                        grant_type = $"client_credentials",
                    };
                    StringContent jsonContent = new StringContent(JsonConvert.SerializeObject(user));
                    var response = await client.PostAsync("v2/oauth/token", jsonContent, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(Auth)}] Get result auth response {jsonResponse}");
                    Token = JsonConvert.DeserializeObject<KasikornAuthResponse>(jsonResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                PluginContext.Log.Error(ex.ToString());
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
            }
        }

        public async Task<KasikornGenerateQrResponse> GenerateQRCode(decimal amount, CancellationToken cancellationToken)
        {
            try
            {
                if (tryCount > settings.Kasikorn.RetriesCount)
                {
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GenerateQRCode)}] Retiries end for GenerateQRCode. Amount = {amount}");
                    throw new Exception("Retries for generate qr end");
                }
                if (token is null)
                    Auth(cancellationToken);

                PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GenerateQRCode)}] Start GenerateQRCode. Amount = {amount}");
                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

#if DEBUG
                    client.DefaultRequestHeaders.Add("env-id", "QR002");
#endif

                    var request = new KasikornGenerateQrRequest
                    {
                        merchantId = settings.Kasikorn.MerchantId,
                        partnerId = settings.Kasikorn.PartnerId,
                        partnerSecret = settings.Kasikorn.PartnerSecret,
                        qrType = 3,
                        txnAmount = amount.ToString(),
                        txnCurrencyCode = "THB"
                    };
                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                    DateTime nowInThailand = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    request.requestDt = $"{nowInThailand:s}+07:00";

#if DEBUG
                    request.partnerTxnUid = "PARTNERTEST0001";
                    request.reference1 = "INV001";
                    request.reference2 = "HELLOWORLD";
                    request.reference3 = "INV001";
                    request.reference4 = "INV001";
#else
                    request.partnerTxnUid = "PARTNERTEST0001";
                    request.reference1 = "INV001";
#endif

                    StringContent jsonContent = new StringContent(JsonConvert.SerializeObject(request));
                    var response = await client.PostAsync("/v1/qrpayment/request", jsonContent, cancellationToken);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GenerateQRCode)}] Token is expired. TryCount = {tryCount}");
                        tryCount++;
                        ClearToken();
                        return await GenerateQRCode(amount, cancellationToken);
                    }

                    if (tryCount > 0)
                        tryCount = 0;

                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GenerateQRCode)}] Get result GenerateQRCode response {jsonResponse}");
                    return JsonConvert.DeserializeObject<KasikornGenerateQrResponse>(jsonResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
        }

        public async Task<KasikornStatusQrResponse> GetStatusQrCode(string origPartnerTxnUid, CancellationToken cancellationToken)
        {
            try
            {
                if (tryCount > settings.Kasikorn.RetriesCount)
                {
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GetStatusQrCode)}] Retries end for GetStatusQr.");
                    throw new Exception("Retries for get status qr end");
                }
                if (token is null)
                    Auth(cancellationToken);

                PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GetStatusQrCode)}] Start GetStatusQr.");
                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

#if DEBUG
                    client.DefaultRequestHeaders.Add("env-id", "QR002");
#endif

                    var request = new KasikornCancelQrRequest
                    {
                        merchantId = settings.Kasikorn.MerchantId,
                        partnerId = settings.Kasikorn.PartnerId,
                        partnerSecret = settings.Kasikorn.PartnerSecret,
                        origPartnerTxnUid = origPartnerTxnUid
                    };
                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                    DateTime nowInThailand = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    request.requestDt = $"{nowInThailand:s}+07:00";

#if DEBUG
                    request.partnerTxnUid = "PARTNERTEST0002";
#else
                    request.partnerTxnUid = "PARTNERTEST0002";
#endif

                    StringContent jsonContent = new StringContent(JsonConvert.SerializeObject(request));
                    var response = await client.PostAsync("/v1/qrpayment/v4/inquiry", jsonContent, cancellationToken);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GetStatusQrCode)}] Token is expired. TryCount = {tryCount}");
                        tryCount++;
                        ClearToken();
                        return await GetStatusQrCode(origPartnerTxnUid, cancellationToken);
                    }

                    if (tryCount > 0)
                        tryCount = 0;

                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(GetStatusQrCode)}] Get result GetStatusQr response {jsonResponse}");
                    return JsonConvert.DeserializeObject<KasikornStatusQrResponse>(jsonResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
        }

        public async Task<KasikornCancelQrResponse> CancelQrCode(string origPartnerTxnUid, CancellationToken cancellationToken)
        {
            try
            {
                if (tryCount > settings.Kasikorn.RetriesCount)
                {
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(CancelQrCode)}] Retries end for CancelQr.");
                    throw new Exception("Retries for get status qr end");
                }
                if (token is null)
                    Auth(cancellationToken);

                PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(CancelQrCode)}] Start CancelQr.");
                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

#if DEBUG
                    client.DefaultRequestHeaders.Add("env-id", "QR008");
#endif

                    var request = new KasikornStatusQrRequest
                    {
                        merchantId = settings.Kasikorn.MerchantId,
                        partnerId = settings.Kasikorn.PartnerId,
                        partnerSecret = settings.Kasikorn.PartnerSecret,
                        origPartnerTxnUid = origPartnerTxnUid

                    };
                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                    DateTime nowInThailand = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    request.requestDt = $"{nowInThailand:s}+07:00";

#if DEBUG
                    request.partnerTxnUid = "PARTNERTEST0002";
#else
                    request.partnerTxnUid = "PARTNERTEST0002";
#endif

                    StringContent jsonContent = new StringContent(JsonConvert.SerializeObject(request));
                    var response = await client.PostAsync("/v1/qrpayment/cancel", jsonContent, cancellationToken);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(CancelQrCode)}] Token is expired. TryCount = {tryCount}");
                        tryCount++;
                        ClearToken();
                        return await CancelQrCode(origPartnerTxnUid, cancellationToken);
                    }

                    if (tryCount > 0)
                        tryCount = 0;

                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(CancelQrCode)}] Get result CancelQr response {jsonResponse}");
                    return JsonConvert.DeserializeObject<KasikornCancelQrResponse>(jsonResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
        }

        public async Task<KasikornVoidQrResponse> VoidQrCode(string origPartnerTxnUid, CancellationToken cancellationToken)
        {
            try
            {
                if (tryCount > settings.Kasikorn.RetriesCount)
                {
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(VoidQrCode)}] Retries end for VoidQr.");
                    throw new Exception("Retries for get status qr end");
                }
                if (token is null)
                    Auth(cancellationToken);

                PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(VoidQrCode)}] Start VoidQr.");
                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

#if DEBUG
                    client.DefaultRequestHeaders.Add("env-id", "QR012");
#endif

                    var request = new KasikornStatusQrRequest
                    {
                        merchantId = settings.Kasikorn.MerchantId,
                        partnerId = settings.Kasikorn.PartnerId,
                        partnerSecret = settings.Kasikorn.PartnerSecret,
                        origPartnerTxnUid = origPartnerTxnUid

                    };
                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
                    DateTime nowInThailand = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    request.requestDt = $"{nowInThailand:s}+07:00";

#if DEBUG
                    request.partnerTxnUid = "PARTNERTEST0009";
#else
                    request.partnerTxnUid = "PARTNERTEST0009";
#endif

                    StringContent jsonContent = new StringContent(JsonConvert.SerializeObject(request));
                    var response = await client.PostAsync("/v1/qrpayment/void", jsonContent, cancellationToken);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(VoidQrCode)}] Token is expired. TryCount = {tryCount}");
                        tryCount++;
                        ClearToken();
                        return await VoidQrCode(origPartnerTxnUid, cancellationToken);
                    }

                    if (tryCount > 0)
                        tryCount = 0;

                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(VoidQrCode)}] Get result VoidQr response {jsonResponse}");
                    return JsonConvert.DeserializeObject<KasikornVoidQrResponse>(jsonResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
        }

        private void StartTokenExpiresTimer()
        {
            tokenExpiresTimer = new System.Timers.Timer(expiresIn * 1000);
            tokenExpiresTimer.Elapsed += TimerEvent;
            tokenExpiresTimer.AutoReset = true;
            tokenExpiresTimer.Enabled = true;
        }

        private void TimerEvent(object sender, ElapsedEventArgs e)
        {
            PluginContext.Log.Info($"[{nameof(KasikornBankApiService)}|{nameof(TimerEvent)}] Token is expired. Clear token");
            ClearToken();
        }

        private void ClearToken()
        {
            lock (locker)
            {
                if (Token is null)
                    return;

                Token = null;
                tokenExpiresTimer?.Stop();
                tokenExpiresTimer?.Dispose();
                tokenExpiresTimer = null;
            }
        }

        public void Dispose()
        {
            ClearToken();
        }
    }
}
