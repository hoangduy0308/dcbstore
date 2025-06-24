using System.Collections.Generic;
using DCBStore.Models;

namespace DCBStore.Models.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Product> FeaturedProducts { get; set; }
        public IEnumerable<Product> NewArrivals { get; set; }
        
    }
}