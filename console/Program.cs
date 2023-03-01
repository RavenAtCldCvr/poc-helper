using System.Collections.Specialized;
using System.CommandLine;
using System.Globalization;
using System.Net.Http.Headers;
using System.Web;
using Bogus;
using CsvHelper;

namespace console;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Option<FileInfo?> csvFileOption = new(
            "--csvSrcFile",
            description: "The CSV file path for data preparation",
            isDefault: true,
            parseArgument: result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return new FileInfo("default.txt");
                }

                string filePath = result.Tokens.Single().Value;
                if (File.Exists(filePath))
                {
                    return new FileInfo(filePath);
                }

                result.ErrorMessage = "CSV Source File does not exist";
                return null;
            });

        Option<FileInfo?> destFileOption = new(
            "--csvDestFile",
            "Output path of the new CSV file"
        );

        Option<string> columnsOption = new(
            "--columns",
            description: "column names separated by commas",
            isDefault: true,
            parseArgument: result => result.Tokens.Count == 0 ? string.Empty : result.Tokens.Single().Value);

        RootCommand rootCommand = new("POC Helper");

        var csvCommand = new Command("csv", "Work with CSV file");
        rootCommand.AddCommand(csvCommand);

        var mockCommand = new Command("mock", "Add realistic looking columns to the file")
        {
            csvFileOption, destFileOption
        };
        csvCommand.AddCommand(mockCommand);
        mockCommand.SetHandler((csvFile, destFile) =>
        {
            IList<dynamic> records = ReadCsvAsRecords(csvFile!);
            AppendMockColumns(records);
            OverrideFileWithNewRecords(records, destFile!);
        }, csvFileOption, destFileOption);


        var removeColumnsCommand = new Command("remove-columns", "Remove Columns from the file")
        {
            csvFileOption, destFileOption, columnsOption
        };
        csvCommand.AddCommand(removeColumnsCommand);
        removeColumnsCommand.SetHandler((csvFile, destFile, columns) =>
        {
            IList<dynamic> records = ReadCsvAsRecords(csvFile!);
            string[] columnsToRemove = columns.Split(',');
            RemoveColumnsFromRecords(records, columnsToRemove);
            OverrideFileWithNewRecords(records, destFile!);
        }, csvFileOption, destFileOption, columnsOption);

        var removeValuesFromColumnsCommand = new Command("remove-values", "Remove values from specified columns")
        {
            csvFileOption, destFileOption, columnsOption
        };
        csvCommand.AddCommand(removeValuesFromColumnsCommand);
        removeValuesFromColumnsCommand.SetHandler((csvFile, destFile, columns) =>
        {
            IList<dynamic> records = ReadCsvAsRecords(csvFile!);
            string[] columnsToRemoveValues = columns.Split(',');
            RemoveValuesFromColumns(records, columnsToRemoveValues);
            OverrideFileWithNewRecords(records, destFile!);
        }, csvFileOption, destFileOption, columnsOption);

        var geoEncodingCommand = new Command("geocoding",
            "Concatenate a single zipcode columns, and add new lat, long columns")  {
            csvFileOption,destFileOption,columnsOption
        };
        csvCommand.AddCommand(geoEncodingCommand);

        // Geocoding Command Not tested for batching, but tested for single invocation.
        geoEncodingCommand.SetHandler( (csvFile, destFile, columns) =>
        {
            IList<dynamic> records = ReadCsvAsRecords(csvFile!);
            // we will reuse httpclient per console command
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            IEnumerable<string> responses = GetGeoLocation(client, records, columns, 250, TimeSpan.FromSeconds(65));
            foreach (string response in responses)
            {
                Console.WriteLine(response);
            }
            OverrideFileWithNewRecords(records, destFile!);
        }, csvFileOption, destFileOption, columnsOption);

        return await rootCommand.InvokeAsync(args);
    }

    // untested code
    static IEnumerable<string> GetGeoLocation(HttpClient client, IList<dynamic> records, string zipCodeColumn, int batchSize, TimeSpan coolDownDuration)
    {

        if (batchSize < 1)
        {
            throw new InvalidOperationException($"{nameof(batchSize)} cannot be less than 1");
        }

        if (zipCodeColumn.Split(',').Length != 1)
        {
            throw new ArgumentException("Must be a single column name for zipcode.", nameof(zipCodeColumn));
        }

        var data = new List<string>();

        for (int index = 0; index < records.Count; index++)
        {
            Task<HttpResponseMessage> response = InvokeGeocodingApi(client, records, zipCodeColumn, index);
            string content = response.Result.Content.ReadAsStringAsync().Result;
            data.Add(content);

            if (index % batchSize == 0)
            {
                Thread.Sleep(coolDownDuration);
            }
        }

        return data;
    }

    private static async Task<HttpResponseMessage> InvokeGeocodingApi(HttpClient client, IList<dynamic> records, string zipCodeColumn, int index)
    {
        var builder = new UriBuilder("https://developers.onemap.sg/commonapi/search");
        NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
        ((IDictionary<string, object?>) records[index]).TryGetValue(zipCodeColumn, out object? zipcode);
        query["searchVal"] = zipcode == null ? string.Empty : zipcode.ToString()?.Trim();
        query["returnGeom"] = "Y";
        query["getAddrDetails"] = "N";
        query["pageNum"] = "1";
        builder.Query = query.ToString();
        string url = builder.ToString();
        return await client.GetAsync(url);

    }

    private static void RemoveValuesFromColumns(IList<dynamic> records, string[] columns)
    {
        if (records == null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        foreach (dynamic record in records)
        {
            var row = (IDictionary<string, object>)record;
            foreach (string columnName in columns)
            {
                if (row.ContainsKey(columnName))
                {
                    row[columnName] = string.Empty;
                }
            }
        }
    }

    private static void RemoveColumnsFromRecords(IList<dynamic> records, string[] columns)
    {
        if (records == null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        foreach (dynamic record in records)
        {
            var row = (IDictionary<string, object>)record;
            foreach (string columnName in columns)
            {
                bool isSuccessful = row.Remove(columnName);
                if (!isSuccessful)
                {
                    Console.WriteLine($"Failed to remove column: {columnName}");
                }
            }
        }
    }

    private static IList<dynamic> ReadCsvAsRecords(FileSystemInfo csvFile)
    {
        using StreamReader reader = new(csvFile.FullName);
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<dynamic>().ToList();
    }

    private static void AppendMockColumns(IEnumerable<dynamic> csvRecords)
    {
        if (csvRecords == null)
        {
            throw new ArgumentNullException(nameof(csvRecords));
        }

        Randomizer.Seed = new Random(1233212);
        Faker faker = new();
        foreach (dynamic record in csvRecords)
        {
            record.FirstName = faker.Name.FirstName();
            record.LastName = faker.Name.LastName();
            record.Address = faker.Address.FullAddress();
            record.Country = faker.Address.CountryCode();
            record.ZipCode = faker.Address.ZipCode();
        }
    }

    private static void OverrideFileWithNewRecords(IEnumerable<dynamic> csvRecords, FileInfo destCsvFileInfo)
    {
        if (csvRecords == null)
        {
            throw new ArgumentNullException(nameof(csvRecords));
        }

        if (destCsvFileInfo == null)
        {
            throw new ArgumentNullException(nameof(destCsvFileInfo));
        }

        using FileStream stream = File.Open(destCsvFileInfo.FullName, FileMode.Create);
        using StreamWriter writer = new(stream);
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(csvRecords);
    }
}
