using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class CheckoutVM
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(50, ErrorMessage = "Full name must not exceed 50 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        [StringLength(60, ErrorMessage = "Address must not exceed 60 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(24, ErrorMessage = "Phone number must not exceed 24 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Note")]
        [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")]
        public string? Note { get; set; }

        [Display(Name = "Use Familiar Customer Information")]
        public bool UseFamiliarInfo { get; set; }

        // For display cart items
        public List<CartItemVM> CartItems { get; set; } = new List<CartItemVM>();
        public double TotalAmount { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "COD";
    }
}
