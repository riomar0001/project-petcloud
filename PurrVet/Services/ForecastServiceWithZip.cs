using Microsoft.ML;
using PetCloud.Models;

namespace PetCloud.Services {
    public class ForecastServiceWithZip {
        private readonly MLContext _mlContext;
        private readonly string _modelFolder;
        private readonly Dictionary<string, ITransformer> _models = new();

        private static readonly Dictionary<string, string> ServiceNameMap = new()
        {
            { "Confinement / Hospitalization", "Confinement" },
            { "Deworming & Preventives", "Deworming" },
            { "End of Life Care", "EndOfLifeCare" },
            { "Grooming & Wellness", "Grooming" },
            { "Medication & Treatment", "Medication" },
            { "Professional Fee / Consultation", "Consultation" },
            { "Specialty Tests / Rare Cases", "SpecialtyTests" },
            { "Surgery", "Surgery" },
            { "Vaccination", "Vaccination" },
            { "Diagnostics & Laboratory Tests", "Diagnostics" }
        };

        private readonly string[] _allServices = new[]
        {
            "Consultation", "Vaccination", "Grooming", "Deworming",
            "Surgery", "Medication", "SpecialtyTests",
            "EndOfLifeCare", "Confinement", "Diagnostics"
        };

        public ForecastServiceWithZip(string modelFolder) {
            _mlContext = new MLContext();
            _modelFolder = modelFolder;

            LoadModels();
        }

        private void LoadModels() {
            foreach (var service in _allServices) {
                var path = Path.Combine(_modelFolder, service + ".zip");
                if (File.Exists(path)) {
                    var model = _mlContext.Model.Load(path, out _);
                    _models[service] = model;
                    Console.WriteLine($"Loaded {service} model from {path}");
                } else {
                    Console.WriteLine($"Model {service}.zip not found at {path}");
                }
            }
        }

        private List<float> GetServiceHistory(List<ServiceDemandData> records, string service) {
            return records.Select(r => service switch {
                "Consultation" => r.Consultation,
                "Vaccination" => r.Vaccination,
                "Grooming" => r.Grooming,
                "Deworming" => r.Deworming,
                "Surgery" => r.Surgery,
                "Medication" => r.Medication,
                "SpecialtyTests" => r.SpecialtyTests,
                "EndOfLifeCare" => r.EndOfLifeCare,
                "Confinement" => r.Confinement,
                "Diagnostics" => r.Diagnostics,
                _ => 0
            }).ToList();
        }

        public float Predict(ServiceDemandData input, string service) {
            // handle "All" as sum of all services
            if (service == "All") {
                float sum = 0;
                foreach (var s in _allServices) {
                    if (!_models.ContainsKey(s)) continue;
                    var engine = _mlContext.Model.CreatePredictionEngine<ServiceDemandData, ServiceDemandPrediction>(_models[s]);
                    sum += engine.Predict(input).Score;
                }
                return sum;
            }

            // map ui names for models
            if (!ServiceNameMap.TryGetValue(service, out string mappedService))
                mappedService = "Consultation";

            if (!_models.ContainsKey(mappedService))
                throw new Exception($"Model for {mappedService} not loaded!");

            var predEngine = _mlContext.Model.CreatePredictionEngine<ServiceDemandData, ServiceDemandPrediction>(
                _models[mappedService]
            );

            return predEngine.Predict(input).Score;
        }

        public ForecastResult GetForecastData(List<ServiceDemandData> records, string service, string selectedYearStr) {
            var today = DateTime.Today;

            // filter records up to current month
            if (today.Day < 28) {
                records = records.Where(r => (r.Year < today.Year) ||
                                             (r.Year == today.Year && r.Month <= today.Month)).ToList();
            }

            // cmpute demand history
            List<float> demandHistory;
            if (service == "All") {
                demandHistory = records.Select(r =>
                    r.Consultation + r.Vaccination + r.Grooming +
                    r.Deworming + r.Surgery + r.Medication +
                    r.SpecialtyTests + r.EndOfLifeCare + r.Confinement + r.Diagnostics
                ).ToList();
            } else {
                demandHistory = GetServiceHistory(records, ServiceNameMap.ContainsKey(service) ? ServiceNameMap[service] : service);
            }

            if (!records.Any() || demandHistory.Count < 6)
                return new ForecastResult { Severity = "NO DATA" };

            // compute predicted counts for existing data
            var predictedCounts = records.Select(r => (double)Predict(r, service)).ToList();

            // determine target month/year
            var last = records.Last();
            int targetMonth = last.Month;
            int targetYear = last.Year;

            // future forecast parameters 12 months
            int globalHorizon = 12;
            int selectedYear = 0;
            bool yearParsed = int.TryParse(selectedYearStr, out selectedYear);

            int chartHorizon = !yearParsed || selectedYearStr == "All" ? 12
                                : selectedYear < today.Year ? 0
                                : selectedYear == today.Year ? Math.Max(0, 13 - today.Month)
                                : 12;

            var globalFutureForecasts = new List<double>();
            var forecastMonths = new List<string>();
            var globalSeverities = new List<string>();

            float lag1 = demandHistory.Last();
            float lag2 = demandHistory.Count > 1 ? demandHistory[^2] : lag1;
            float lag3 = demandHistory.Count > 2 ? demandHistory[^3] : lag2;

            double avg = demandHistory.Average();
            double stdDev = Math.Sqrt(demandHistory.Average(v => Math.Pow(v - avg, 2)));
            double highThreshold = avg + 0.5 * stdDev;
            double lowThreshold = avg - 0.5 * stdDev;

            int gTargetMonth = targetMonth;
            int gTargetYear = targetYear;

            for (int i = 0; i < globalHorizon; i++) {
                gTargetMonth++;
                if (gTargetMonth > 12) {
                    gTargetMonth = 1;
                    gTargetYear++;
                }

                var nextInput = new ServiceDemandData {
                    Year = gTargetYear,
                    Month = gTargetMonth,
                    Month_sin = (float)Math.Sin(2 * Math.PI * gTargetMonth / 12.0),
                    Month_cos = (float)Math.Cos(2 * Math.PI * gTargetMonth / 12.0),
                    IsPeakSeason = (gTargetMonth == 4 || gTargetMonth == 5 || gTargetMonth == 12) ? 1 : 0,
                    IsSlowSeason = (gTargetMonth == 7 || gTargetMonth == 8 || gTargetMonth == 9 || gTargetMonth == 10) ? 1 : 0,
                    IsHoliday = (gTargetMonth == 12) ? 1 : 0,
                    Lag1_Total = lag1,
                    Lag2_Total = lag2,
                    Lag3_Total = lag3,
                    Rolling3_Total = (lag1 + lag2 + lag3) / 3f,
                    Rolling6_Total = demandHistory.TakeLast(6).Append(lag1).Average()
                };

                double forecastValue = Predict(nextInput, service);

                lag3 = lag2;
                lag2 = lag1;
                lag1 = (float)forecastValue;

                globalFutureForecasts.Add(forecastValue);
                forecastMonths.Add(new DateTime(gTargetYear, gTargetMonth, 1).ToString("MMM yyyy"));

                string sev;
                if (forecastValue > highThreshold) sev = "HIGH";
                else if (forecastValue < lowThreshold) sev = "LOW";
                else sev = "MEDIUM";

                globalSeverities.Add(sev);
            }

            // filter months by selected year
            var allMonths = records.Select(r => $"{r.Month}/{r.Year}").ToList();
            var allActuals = demandHistory.Select(d => (double)d).ToList();
            var allPredictions = predictedCounts.ToList();

            List<string> monthsToShow = allMonths;
            List<double> actualToShow = allActuals;
            List<double> predictedToShow = allPredictions;

            if (yearParsed && selectedYearStr != "All") {
                monthsToShow = allMonths.Where(m => m.EndsWith("/" + selectedYear)).ToList();
                actualToShow = allActuals.Where((_, i) => allMonths[i].EndsWith("/" + selectedYear)).ToList();
                predictedToShow = allPredictions.Where((_, i) => allMonths[i].EndsWith("/" + selectedYear)).ToList();
            }

            var futureForecastsForChart = globalFutureForecasts.Take(chartHorizon).ToList();
            var nextMonthLabel = forecastMonths.FirstOrDefault() ?? "";
            var nextForecastValue = globalFutureForecasts.FirstOrDefault();
            var severity = globalFutureForecasts.Any()
                ? (globalFutureForecasts.First() > highThreshold ? "HIGH" :
                   globalFutureForecasts.First() < lowThreshold ? "LOW" : "MEDIUM")
                : "NONE";

            // ranking
            var nextMonthServiceRanking = new List<ServiceForecastResult>();
            int nextMonth = targetMonth + 1;
            int nextYear = targetYear;
            if (nextMonth > 12) {
                nextMonth = 1;
                nextYear++;
            }

            foreach (var s in _allServices) {
                double value = PredictNextMonth(records, s, nextYear, nextMonth);
                nextMonthServiceRanking.Add(new ServiceForecastResult {
                    Service = s,
                    Count = Math.Round(value, 2)
                });
            }

            nextMonthServiceRanking = nextMonthServiceRanking.OrderByDescending(x => x.Count).ToList();

            return new ForecastResult {
                Months = monthsToShow,
                ActualCounts = actualToShow,
                PredictedCounts = predictedToShow,
                FutureForecasts = futureForecastsForChart,
                GlobalFutureForecasts = globalFutureForecasts,
                ForecastMonths = forecastMonths,
                GlobalSeverities = globalSeverities,
                NextMonth = nextMonthLabel,
                NextForecastValue = nextForecastValue,
                Severity = severity,
                NextMonthServiceRanking = nextMonthServiceRanking
            };
        }

        private double PredictNextMonth(List<ServiceDemandData> records, string serviceName, int nextYear, int nextMonth) {
            var demandHistory = GetServiceHistory(records, serviceName);
            if (demandHistory.Count < 3) return 0;

            float lag1 = demandHistory[^1];
            float lag2 = demandHistory[^2];
            float lag3 = demandHistory[^3];

            var input = new ServiceDemandData {
                Year = nextYear,
                Month = nextMonth,
                Month_sin = (float)Math.Sin(2 * Math.PI * nextMonth / 12.0),
                Month_cos = (float)Math.Cos(2 * Math.PI * nextMonth / 12.0),
                IsPeakSeason = (nextMonth == 4 || nextMonth == 5 || nextMonth == 12) ? 1 : 0,
                IsSlowSeason = (nextMonth == 7 || nextMonth == 8 || nextMonth == 9 || nextMonth == 10) ? 1 : 0,
                IsHoliday = (nextMonth == 12) ? 1 : 0,
                Lag1_Total = lag1,
                Lag2_Total = lag2,
                Lag3_Total = lag3,
                Rolling3_Total = (lag1 + lag2 + lag3) / 3f,
                Rolling6_Total = demandHistory.TakeLast(6).Append(lag1).Average()
            };

            return Predict(input, serviceName);
        }
    }
}
