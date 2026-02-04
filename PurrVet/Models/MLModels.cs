using Microsoft.ML.Data;

namespace PurrVet.Services {
    public class ServiceDemandData {
        public int Year { get; set; }
        public int Month { get; set; }
        public float Month_sin { get; set; }
        public float Month_cos { get; set; }
        public float IsPeakSeason { get; set; }
        public float IsSlowSeason { get; set; }
        public float IsHoliday { get; set; }
        public float Lag1_Total { get; set; }
        public float Lag2_Total { get; set; }
        public float Lag3_Total { get; set; }
        public float Rolling3_Total { get; set; }
        public float Rolling6_Total { get; set; }

        public float Consultation { get; set; }
        public float Vaccination { get; set; }
        public float Grooming { get; set; }
        public float Deworming { get; set; }
        public float Surgery { get; set; }
        public float Medication { get; set; }
        public float SpecialtyTests { get; set; }
        public float EndOfLifeCare { get; set; }
        public float Confinement { get; set; }
        public float Diagnostics { get; set; }
        public float GetServiceCount(string service) {
            switch (service) {
                case "Confinement / Hospitalization": return Confinement;
                case "Deworming & Preventives": return Deworming;
                case "End of Life Care": return EndOfLifeCare;
                case "Grooming & Wellness": return Grooming;
                case "Medication & Treatment": return Medication;
                case "Professional Fee / Consultation": return Consultation;
                case "Specialty Tests / Rare Cases": return SpecialtyTests;
                case "Surgery": return Surgery;
                case "Vaccination": return Vaccination;
                case "Diagnostics & Laboratory Tests": return Diagnostics;

                default: return 0;
            }
        }

    }

    public class ServiceDemandPrediction {
        [ColumnName("Score")]
        public float Score { get; set; }
    }

}
