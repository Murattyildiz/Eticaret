using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eticaret.Models
{
    public class Product
    {
        public Product()
        {
            CreatedDate = DateTime.Now;
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(100, ErrorMessage = "Ürün adı en fazla 100 karakter olabilir")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı 0 veya daha büyük olmalıdır")]
        public int Stock { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedDate { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur")]
        [ForeignKey("Category")]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kategori seçin")]
        public int CategoryId { get; set; }

        public virtual Category Category { get; set; }
    }
} 