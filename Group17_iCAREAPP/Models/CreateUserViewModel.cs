// The CreateUserViewModel class is the viewmodel that manages the data required for the user creation form.
// This class contains the user's name, username, password, profession, and account expiration date. 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Group17_iCAREAPP.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Profession")]
        public string Profession { get; set; }

        [Display(Name = "Account Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? AccountExpiryDate { get; set; }
    }
}