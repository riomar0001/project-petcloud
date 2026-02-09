using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
using PetCloud.Models;
using static PurrVet.Controllers.AdminController;

namespace PetCloud.Services
{
    public class ForecastResult
    {
        public List<string> Months { get; set; } = new();
        public List<double> ActualCounts { get; set; } = new();
        public List<double> PredictedCounts { get; set; } = new();
        public List<double> FutureForecasts { get; set; } = new();
        public List<double> GlobalFutureForecasts { get; set; } = new();
        public List<string> ForecastMonths { get; set; } = new();
        public List<string> GlobalSeverities { get; set; } = new();
        public List<ServiceForecastResult> NextMonthServiceRanking { get; set; } = new();

        public string NextMonth { get; set; } = string.Empty;
        public double NextForecastValue { get; set; } = 0;
        public double TotalServicesNextMonth { get; set; }

        public string Severity { get; set; } = "MEDIUM";
    }

    public class ForecastService
    {
        private readonly string _csvPath;
        private readonly MLContext _mlContext;

        public ForecastService(string csvPath)
        {
            _csvPath = csvPath;
            _mlContext = new MLContext();
        }
        private List<float> GetServiceHistory(List<ServiceDemandData> records, string service)
        {
            return records.Select(r => service switch
            {
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
        private double PredictNextMonthForService( List<ServiceDemandData> records, string serviceName, int nextYear, int nextMonth)
        {
            var demandHistory = GetServiceHistory(records, serviceName);
            if (demandHistory.Count < 6) return 0;

            var ml = new MLContext();
            var data = ml.Data.LoadFromEnumerable(records);

            var pipeline = ml.Transforms.Concatenate("Features",
                nameof(ServiceDemandData.Year),
                nameof(ServiceDemandData.Month),
                nameof(ServiceDemandData.Month_sin),
                nameof(ServiceDemandData.Month_cos),
                nameof(ServiceDemandData.IsPeakSeason),
                nameof(ServiceDemandData.IsSlowSeason),
                nameof(ServiceDemandData.IsHoliday),
                nameof(ServiceDemandData.Lag1_Total),
                nameof(ServiceDemandData.Lag2_Total),
                nameof(ServiceDemandData.Lag3_Total),
                nameof(ServiceDemandData.Rolling3_Total),
                nameof(ServiceDemandData.Rolling6_Total)
            )
            .Append(ml.Regression.Trainers.FastTree(
                labelColumnName: serviceName,
                featureColumnName: "Features",
                numberOfTrees: 50,
                numberOfLeaves: 32,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.05));

            var model = pipeline.Fit(data);
            var engine = ml.Model.CreatePredictionEngine<ServiceDemandData, ServiceDemandPrediction>(model);

            float sLag1 = demandHistory[^1];
            float sLag2 = demandHistory[^2];
            float sLag3 = demandHistory[^3];

            var input = new ServiceDemandData
            {
                Year = nextYear,
                Month = nextMonth,
                Month_sin = (float)Math.Sin(2 * Math.PI * nextMonth / 12.0),
                Month_cos = (float)Math.Cos(2 * Math.PI * nextMonth / 12.0),
                IsPeakSeason = (nextMonth == 4 || nextMonth == 5 || nextMonth == 12) ? 1 : 0,
                IsSlowSeason = (nextMonth == 7 || nextMonth == 8 || nextMonth == 9 || nextMonth == 10) ? 1 : 0,
                IsHoliday = (nextMonth == 12) ? 1 : 0,

                Lag1_Total = sLag1,
                Lag2_Total = sLag2,
                Lag3_Total = sLag3,
                Rolling3_Total = (sLag1 + sLag2 + sLag3) / 3f,

                Rolling6_Total = demandHistory
                    .TakeLast(6)
                    .Append(sLag1)
                    .Average()
            };

            return Math.Max(0, engine.Predict(input).Score);
        }


        public ForecastResult TrainAndPredict(string serviceColumn, string selectedYearStr)         // Method pang train model and predict sa values
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            List<ServiceDemandData> records;
            using (var reader = new StreamReader(_csvPath))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<ServiceDemandDataMap>();
                records = csv.GetRecords<ServiceDemandData>().ToList();
            }
            List<ServiceDemandData> trainingRecords = records;

            var today = DateTime.Today;

            if (today.Day < 28)
            {
                records = records.Where(r => (r.Year < today.Year) ||
                                             (r.Year == today.Year && r.Month <= today.Month)).ToList();
            }


            List<float> demandHistory;
            if (serviceColumn == "All") // Kung All services, i-combine tanan
            {
                demandHistory = records.Select(r =>
                    r.Consultation + r.Vaccination + r.Grooming +
                    r.Deworming + r.Surgery + r.Medication +
                    r.SpecialtyTests + r.EndOfLifeCare + r.Confinement + r.Diagnostics
                ).ToList();
            }
            else
            {
                demandHistory = records.Select(r => 
                {
                    return serviceColumn switch  // Kung specific service lang
                    {
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
                        _ => r.Consultation
                    };
                }).ToList();
            }
            // If not enough ang data, return ni
            if (!records.Any() || demandHistory.Count < 6)
            {
                return new ForecastResult { Severity = "NO DATA" };
            }

            if (serviceColumn == "All")
            {
                trainingRecords = records.Select(r => new ServiceDemandData
                {
                    Year = r.Year,
                    Month = r.Month,
                    Month_sin = r.Month_sin,
                    Month_cos = r.Month_cos,
                    IsPeakSeason = r.IsPeakSeason,
                    IsSlowSeason = r.IsSlowSeason,
                    IsHoliday = r.IsHoliday,
                    Lag1_Total = r.Lag1_Total,
                    Lag2_Total = r.Lag2_Total,
                    Lag3_Total = r.Lag3_Total,
                    Rolling3_Total = r.Rolling3_Total,
                    Rolling6_Total = r.Rolling6_Total,

                    // TOTAL SERVICES ONLY FOR "ALL"
                    Consultation =
                        r.Consultation + r.Vaccination + r.Grooming +
                        r.Deworming + r.Surgery + r.Medication +
                        r.SpecialtyTests + r.EndOfLifeCare + r.Confinement + r.Diagnostics
                }).ToList();

                serviceColumn = nameof(ServiceDemandData.Consultation);
            }

            // Mao ni ang sequence sa steps nga buhaton
            var data = _mlContext.Data.LoadFromEnumerable(trainingRecords);
            var pipeline = _mlContext.Transforms.Concatenate("Features",  // gi-combine tanan relevant columns para mahimong usa ka feature vector
                   nameof(ServiceDemandData.Year),
                   nameof(ServiceDemandData.Month),
                   nameof(ServiceDemandData.Month_sin),
                   nameof(ServiceDemandData.Month_cos),
                   nameof(ServiceDemandData.IsPeakSeason),
                   nameof(ServiceDemandData.IsSlowSeason),
                   nameof(ServiceDemandData.IsHoliday),
                   nameof(ServiceDemandData.Lag1_Total),
                   nameof(ServiceDemandData.Lag2_Total),
                   nameof(ServiceDemandData.Lag3_Total),
                   nameof(ServiceDemandData.Rolling3_Total),
                   nameof(ServiceDemandData.Rolling6_Total)
                )
                .Append(_mlContext.Regression.Trainers.FastTree(
                    labelColumnName: serviceColumn,
                    featureColumnName: "Features",
                    numberOfTrees: 50,
                    numberOfLeaves: 32,
                    minimumExampleCountPerLeaf: 10,
                    learningRate: 0.05));
            // split data into 80% training, 20% testing
            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

            var model = pipeline.Fit(data);

            // Evaluate on test set
            var predictions = model.Transform(split.TestSet);
            var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: serviceColumn, scoreColumnName: "Score");

            Console.WriteLine($"R²: {metrics.RSquared:0.###}"); //accuracy
            Console.WriteLine($"MAE: {metrics.MeanAbsoluteError:0.###}"); //average error
            Console.WriteLine($"RMSE: {metrics.RootMeanSquaredError:0.###}"); //

            //prediction engine ni
            var predEngine = _mlContext.Model.CreatePredictionEngine<ServiceDemandData, ServiceDemandPrediction>(model);

            var predictedCounts = records.Select(r => (double)predEngine.Predict(r).Score).ToList();

            var last = records.Last();
            int targetMonth = (int)last.Month;
            int targetYear = (int)last.Year;

            int globalHorizon = 12;

            int chartHorizon = 0;
            int selectedYear = 0;
            bool yearParsed = int.TryParse(selectedYearStr, out selectedYear);

            if (!yearParsed || selectedYearStr == "All")
                chartHorizon = 12;
            else if (selectedYear < today.Year)
                chartHorizon = 0;
            else if (selectedYear == today.Year)
                chartHorizon = Math.Max(0, 13 - today.Month);
            else if (selectedYear > today.Year)
                chartHorizon = 12;

            var globalFutureForecasts = new List<double>();
            var forecastMonths = new List<string>();
            var globalSeverities = new List<string>();

            float lag1 = demandHistory.Last();
            float lag2 = demandHistory.Count > 1 ? demandHistory[^2] : lag1;    
            float lag3 = demandHistory.Count > 2 ? demandHistory[^3] : lag2;

            int gTargetMonth = targetMonth;
            int gTargetYear = targetYear;

            double avg = demandHistory.Average();
            double stdDev = Math.Sqrt(demandHistory.Average(v => Math.Pow(v - avg, 2)));

            double highThreshold = avg + (0.5 * stdDev);
            double lowThreshold = avg - (0.5 * stdDev);

            for (int i = 0; i < globalHorizon; i++)
            {
                gTargetMonth++;
                if (gTargetMonth > 12)
                {
                    gTargetMonth = 1;
                    gTargetYear++;
                }

                var nextInput = new ServiceDemandData
                {
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

                var prediction = predEngine.Predict(nextInput);
                float forecastValue = prediction.Score;

                lag3 = lag2;
                lag2 = lag1;
                lag1 = forecastValue;

                globalFutureForecasts.Add(forecastValue);
                forecastMonths.Add(new DateTime(gTargetYear, gTargetMonth, 1).ToString("MMM yyyy"));

                string sev;
                if (forecastValue > highThreshold) sev = "HIGH";
                else if (forecastValue < lowThreshold) sev = "LOW";
                else sev = "MEDIUM";

                globalSeverities.Add(sev);
            }

            var allMonths = records.Select(r => $"{r.Month}/{r.Year}").ToList();
            var allActuals = demandHistory.Select(d => (double)d).ToList();
            var allPredictions = predictedCounts.ToList();

            List<string> monthsToShow = allMonths;
            List<double> actualToShow = allActuals;
            List<double> predictedToShow = allPredictions;

            if (yearParsed && selectedYearStr != "All")
            {
                monthsToShow = allMonths.Where(m => m.EndsWith("/" + selectedYear)).ToList();
                actualToShow = allActuals
                    .Where((_, i) => allMonths[i].EndsWith("/" + selectedYear))
                    .ToList();
                predictedToShow = allPredictions
                    .Where((_, i) => allMonths[i].EndsWith("/" + selectedYear))
                    .ToList();
            }

            var futureForecastsForChart = globalFutureForecasts.Take(chartHorizon).ToList();

            var nextMonthLabel = forecastMonths.FirstOrDefault() ?? "";
            var nextForecastValue = globalFutureForecasts.FirstOrDefault();

            var severity = globalFutureForecasts.Any()
                ? (globalFutureForecasts.First() > highThreshold ? "HIGH" :
                   globalFutureForecasts.First() < lowThreshold ? "LOW" : "MEDIUM")
                : "NONE";
            var serviceTypes = new[] {
                "Consultation", "Vaccination", "Grooming", "Deworming",
                "Surgery", "Medication", "SpecialtyTests",
                "EndOfLifeCare", "Confinement", "Diagnostics"
            };
            var nextMonthServiceRanking = new List<ServiceForecastResult>();

            int nextMonth = targetMonth + 1;
            int nextYear = targetYear;
            if (nextMonth > 12)
            {
                nextMonth = 1;
                nextYear++;
            }

            foreach (var service in serviceTypes)
            {
                double value = PredictNextMonthForService(
                    records,
                    service,
                    nextYear,
                    nextMonth
                );

                nextMonthServiceRanking.Add(new ServiceForecastResult
                {
                    Service = service,
                    Count = Math.Round(value, 2)
                });
            }

            nextMonthServiceRanking = nextMonthServiceRanking
                .OrderByDescending(x => x.Count)
                .ToList();


            return new ForecastResult
            {
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
        public ForecastResult TrainAndPredict(List<ServiceDemandData> mergedRecords, string serviceColumn, string selectedYearStr)
        {
   
            var tempPath = Path.Combine(Path.GetTempPath(), "merged_forecast_data.csv");
            using (var writer = new StreamWriter(tempPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<ServiceDemandDataMap>();
                csv.WriteRecords(mergedRecords);
            }

            var tempService = new ForecastService(tempPath);
            return tempService.TrainAndPredict(serviceColumn, selectedYearStr);
        }

    }
}
