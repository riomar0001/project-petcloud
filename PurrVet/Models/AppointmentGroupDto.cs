namespace PurrVet.Models
{
    public class AppointmentGroupDto
    {
        public int GroupID { get; set; }  
        public List<AppointmentDto> Appointments { get; set; }
    }

    public class AppointmentDto
    {
        public int AppointmentID { get; set; }
        public string PetName { get; set; }
        public string Subtype { get; set; }
        public string Category { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; }
    }   
}
