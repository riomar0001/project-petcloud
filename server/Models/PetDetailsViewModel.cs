namespace PetCloud.Models {
    public class PetDetailsViewModel {
        public Pet Pet { get; set; }
        public List<Appointment> Appointments { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public int? CategoryFilter { get; set; }

        public string AgeInMonths => GetAge(Pet.Birthdate);

        private static string GetAge(DateTime birthdate) {
            var age = DateTime.Now - birthdate;
            var months = (int)(age.TotalDays / 30.44);
            return months < 12 ? $"{months} months old" : $"{months / 12} years old";
        }
    }
}