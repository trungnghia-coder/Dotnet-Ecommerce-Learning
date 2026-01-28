using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;

namespace ECommerceMVC.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }
        public string CreatePaymentUrl(HttpContext context, VnPayRequestModel model)
        {
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", _config["Vnpay:Version"]);
            vnpay.AddRequestData("vnp_Command", _config["Vnpay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _config["Vnpay:TmnCode"]);

            // Convert USD to VND (model.Amount is in USD)
            var exchangeRate = decimal.Parse(_config["Vnpay:ExchangeRate"] ?? "25000");
            var amountInVND = (decimal)model.Amount * exchangeRate;

            // VNPay requires amount in VND * 100 (smallest unit)
            var amount = ((long)(amountInVND * 100)).ToString();
            vnpay.AddRequestData("vnp_Amount", amount);

            vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config["Vnpay:CurrCode"]);
            vnpay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", _config["Vnpay:Locale"] ?? "vn");

            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {model.OrderId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["Vnpay:ReturnUrl"]);
            vnpay.AddRequestData("vnp_TxnRef", model.OrderId.ToString());

            var paymentUrl = vnpay.CreateRequestUrl(_config["Vnpay:BaseUrl"], _config["Vnpay:HashSecret"]);

            return paymentUrl;
        }

        public VnPayResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config["Vnpay:HashSecret"]);

            if (!checkSignature)
            {
                return new VnPayResponseModel { Success = false };
            }

            return new VnPayResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnp_orderId,
                TransactionId = vnp_TransactionId,
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }
}
