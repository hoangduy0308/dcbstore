using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DCBStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}