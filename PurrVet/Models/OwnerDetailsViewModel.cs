using System.Collections.Generic;

namespace PurrVet.Models
{
    public class OwnerDetailsViewModel
    {
        public Owner Owner { get; set; }
        public List<Pet> Pets { get; set; }

        public string? SearchQuery { get; set; }
        public string? TypeFilter { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
