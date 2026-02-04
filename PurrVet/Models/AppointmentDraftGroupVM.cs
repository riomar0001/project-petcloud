namespace PurrVet.Models {
    public class AppointmentDraftGroupVM {
        public string GroupDraftId { get; set; }
        public string GroupDate { get; set; }
        public string GroupTime { get; set; }

        public int? OwnerID { get; set; }

        public List<AppointmentDraft> Drafts { get; set; } = new();

        public List<Owner> Owners { get; set; } = new();
        public List<ServiceCategory> Categories { get; set; } = new();
        public List<ServiceSubtype> Subtypes { get; set; } = new();
        public List<Pet> OwnerPets { get; set; } = new();
    }
}
