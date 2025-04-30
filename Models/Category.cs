using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eticaret.Models
{
    public class Category
    {
        public Category()
        {
            Products = new HashSet<Product>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur")]
        [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir")]
        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<Product> Products { get; set; }
    }
} 