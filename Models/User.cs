using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eticaret.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        public DateTime RegisterDate { get; set; } = DateTime.Now;
        
        [StringLength(20)]
        public string Role { get; set; } = "User"; // Default role is User

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
    }
} 