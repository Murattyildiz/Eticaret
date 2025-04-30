using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eticaret.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Product Product { get; set; }
    }
} 