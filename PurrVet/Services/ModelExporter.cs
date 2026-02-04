using Microsoft.ML;
using PurrVet.Services;

public class ModelExporter {
    private readonly MLContext _mlContext;
    private readonly string _csvPath;
    private readonly string _exportFolder;

    public ModelExporter(string csvPath, string exportFolder) {
        _mlContext = new MLContext();
        _csvPath = csvPath;
        _exportFolder = exportFolder;

        if (!Directory.Exists(_exportFolder))
            Directory.CreateDirectory(_exportFolder);
    }

    public void ExportAllServiceModels() {
        var records = LoadCsv(_csvPath);

        string[] services = new[]
        {
            "Consultation", "Vaccination", "Grooming", "Deworming",
            "Surgery", "Medication", "SpecialtyTests",
            "EndOfLifeCare", "Confinement", "Diagnostics"
        };

        foreach (var service in services) {
            var model = TrainServiceModel(records, service);
            var path = Path.Combine(_exportFolder, $"{service}.zip");
            _mlContext.Model.Save(model, _mlContext.Data.LoadFromEnumerable(records).Schema, path);
            Console.WriteLine($"Saved {service} model to {path}");
        }
    }

    private List<ServiceDemandData> LoadCsv(string path) {
        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvHelper.CsvReader(reader, config);
        csv.Context.RegisterClassMap<ServiceDemandDataMap>();
        return csv.GetRecords<ServiceDemandData>().ToList();
    }

    private ITransformer TrainServiceModel(List<ServiceDemandData> records, string serviceColumn) {
        var data = _mlContext.Data.LoadFromEnumerable(records);

        var pipeline = _mlContext.Transforms
        .Conversion.ConvertType(nameof(ServiceDemandData.Year), outputKind: Microsoft.ML.Data.DataKind.Single)
        .Append(_mlContext.Transforms
        .Conversion.ConvertType(nameof(ServiceDemandData.Month), outputKind: Microsoft.ML.Data.DataKind.Single))
        .Append(_mlContext.Transforms.Concatenate("Features",
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
        ))
        .Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: serviceColumn,
            featureColumnName: "Features",
            numberOfTrees: 50,
            numberOfLeaves: 32,
            minimumExampleCountPerLeaf: 10,
            learningRate: 0.05));


        return pipeline.Fit(data);
    }
}
