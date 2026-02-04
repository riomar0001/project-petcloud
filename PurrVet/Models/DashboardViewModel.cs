namespace PurrVet.Models {
    public class DashboardViewModel {
        public int TotalUsers { get; set; }
        public int TotalPets { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalCategory { get; set; }
        public int TotalType { get; set; }


        public List<string> Months { get; set; } = new();

        public List<double> ServiceCounts { get; set; } = new();

        public List<double> PredictedCounts { get; set; } = new();

        public List<double> FutureForecasts { get; set; } = new();

        public double RegressionSlope { get; set; }
        public double RegressionIntercept { get; set; }

        public double PredictedNextValue { get; set; }
        public string PredictedMonthLabel { get; set; } = string.Empty;
        public string ForecastSeverity { get; set; } = "MEDIUM";

        public Dictionary<string, double> ForecastHistory { get; set; } = new();
        public List<int> Years { get; set; } = new();
    }
}
