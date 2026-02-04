namespace PurrVet.Models {
    public class PetCardVM {
        public Pet Pet { get; set; }
        public List<Appointment> Records { get; set; }
        public List<Appointment> PageData { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int AgeInMonths { get; set; }
    }

}
