using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace MvcForum.Models
{
    public class LogonViewModel : MasterViewModel
    {
        [Required(ErrorMessage="Required field")]
        [DisplayName("Username")]
        [StringLength(25, ErrorMessage="Maximum of 25 characters")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Required field")]
        [DisplayName("Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class RegisterViewModel : MasterViewModel
    {
        public int PasswordMinLength { get; set; }

        [Required(ErrorMessage = "Required field")]
        [DisplayName("Username")]
        [RegularExpression(@"^\w[-\w= ]*\w$", ErrorMessage = "Invalid Username")]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "Too short")]
        [Remote("UserNameAvailable", "Account", ErrorMessage="Username already exists")]
        public string Reg_Username { get; set; }

        [DisplayName("Email")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", ErrorMessage="Not a valid email address")]
        [Required(ErrorMessage = "Required field")]
        [StringLength(70, ErrorMessage = "Maximum of 70 characters")]
        public string Reg_Email { get; set; }

        [Required(ErrorMessage = "Required field")]
        [DisplayName("Password")]
        [DataType(DataType.Password)]
        [StringLength(150, MinimumLength = 6, ErrorMessage = "Minimum of 6 Characters")]
        [Compare("Reg_Password", ErrorMessage = "Passwords do not match")]
        public string Reg_Password { get; set; }

        [Required(ErrorMessage = "Required field")]
        [DisplayName("Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Reg_Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}