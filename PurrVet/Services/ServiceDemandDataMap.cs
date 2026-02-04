using CsvHelper.Configuration;

namespace PurrVet.Services {
    public class ServiceDemandDataMap : ClassMap<ServiceDemandData> {
        public ServiceDemandDataMap() {
            Map(m => m.Year).Name("Year");
            Map(m => m.Month).Name("Month");
            Map(m => m.Month_sin).Name("Month_sin");
            Map(m => m.Month_cos).Name("Month_cos");
            Map(m => m.IsPeakSeason).Name("IsPeakSeason");
            Map(m => m.IsSlowSeason).Name("IsSlowSeason");
            Map(m => m.IsHoliday).Name("IsHoliday");

            Map(m => m.Consultation).Name("Professional Fee / Consultation");
            Map(m => m.Vaccination).Name("Vaccination");
            Map(m => m.Grooming).Name("Grooming & Wellness");
            Map(m => m.Deworming).Name("Deworming & Preventives");
            Map(m => m.Surgery).Name("Surgery");
            Map(m => m.Medication).Name("Medication & Treatment");
            Map(m => m.SpecialtyTests).Name("Specialty Tests / Rare Cases");
            Map(m => m.EndOfLifeCare).Name("End of Life Care");
            Map(m => m.Confinement).Name("Confinement / Hospitalization");
            Map(m => m.Diagnostics).Name("Diagnostics & Laboratory Tests");

        }
    }
}