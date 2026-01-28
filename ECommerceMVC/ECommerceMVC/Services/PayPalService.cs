using ECommerceMVC.Helpers;
using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;

namespace ECommerceMVC.Services
{
    public class PayPalService
    {
        private readonly PayPalHttpClient _client;
        private readonly ILogger<PayPalService> _logger;
        private readonly PayPalSettings _settings;

        public PayPalService(IOptions<PayPalSettings> options, ILogger<PayPalService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            PayPalEnvironment environment;
            if (_settings.Mode == "Live")
            {
                environment = new LiveEnvironment(_settings.ClientId, _settings.Secret);
                _logger.LogInformation("PayPal khởi tạo chế độ LIVE.");
            }
            else
            {
                environment = new SandboxEnvironment(_settings.ClientId, _settings.Secret);
                _logger.LogInformation("PayPal khởi tạo chế độ SANDBOX.");
            }

            _client = new PayPalHttpClient(environment);
        }

        public PayPalHttpClient GetClient() => _client;

        public async Task<string?> CreateOrder(decimal amount, string currency = "USD")
        {
            var totalValue = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");

            request.RequestBody(new OrderRequest
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
        {
            new PurchaseUnitRequest
            {
                AmountWithBreakdown = new AmountWithBreakdown
                {
                    CurrencyCode = currency,
                    Value = totalValue
                }
            }
        }
            });

            try
            {
                var response = await _client.Execute(request);

                var result = response.Result<Order>();

                return result.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi PayPal CreateOrder: {0}", ex.Message);
                return null;
            }
        }

        public async Task<bool> CaptureOrder(string orderId)
        {
            var request = new OrdersCaptureRequest(orderId);
            request.RequestBody(new OrderActionRequest());

            try
            {
                var response = await _client.Execute(request);

                var result = response.Result<Order>();

                return result.Status == "COMPLETED";
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi PayPal CaptureOrder: {0}", ex.Message);
                return false;
            }
        }
    }
}
