using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [StringLength(50, ErrorMessage = "Password must be at least 6 characters", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(50, ErrorMessage = "Full name must not exceed 50 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(50, ErrorMessage = "Email must not exceed 50 characters")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(24)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address")]
        [StringLength(60, ErrorMessage = "Address must not exceed 60 characters")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Please select gender")]
        [Display(Name = "Gender")]
        public bool Gender { get; set; } = true; // true = Male, false = Female

        [Required(ErrorMessage = "Date of birth is required")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-18);
    }
}
