using ECommerceMVC.ViewModels;
namespace ECommerceMVC.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPayRequestModel model);
        VnPayResponseModel PaymentExecute(IQueryCollection collections);
    }
}
