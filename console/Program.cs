using System.CommandLine;
using System.ComponentModel;
using System.Globalization;
using Bogus;
using CsvHelper;

namespace console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var csvFileOption = new Option<FileInfo?>(
            name: "--csvSrcFile",
            description: "The CSV file path for data preparation",
            isDefault: true,
            parseArgument: result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return new FileInfo("default.txt");
                }

                var filePath = result.Tokens.Single().Value;
                if (File.Exists(filePath))
                {
                    return new FileInfo(filePath);
                }

                result.ErrorMessage = "CSV Source File does not exist";
                return null;
            });

        var destFileOption = new Option<FileInfo?>(
            name: "--csvDestFile",
            description: "Output path of the new CSV file"
        );

        var columnsOption = new Option<string>(
            name: "--columns",
            description:"column names separated by commas",
            isDefault: true,
            parseArgument: result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return string.Empty;
                }

                return result.Tokens.Single().Value;
            }
        );

        var rootCommand = new RootCommand("POC Helper");
        
        var csvCommand = new Command("csv", "Work with CSV file");
        rootCommand.AddCommand(csvCommand);
        
        var mockCommand = new Command("mock", "Add realistic looking columns to the file")
        {
            csvFileOption,
            destFileOption
        };
        csvCommand.AddCommand(mockCommand);
        mockCommand.SetHandler((csvFile, destFile) =>
        {
            var records = ReadCsvAsRecords(csvFile!);
            AppendMockColumns(records);
            OverrideFileWithNewRecords(records, destFile!);
        }, csvFileOption, destFileOption);

        
        var removeColumnsCommand = new Command("remove-columns", "Remove Columns from the file")
        {
            csvFileOption,
            destFileOption,
            columnsOption
        };
        csvCommand.AddCommand(removeColumnsCommand);
        removeColumnsCommand.SetHandler((csvFile, destFile, columns) =>
        {
            var records = ReadCsvAsRecords(csvFile!);
            var columnsToRemove = columns.Split(',');
            RemoveColumnsFromRecords(records, columnsToRemove);
            OverrideFileWithNewRecords(records, destFile!);
        }, csvFileOption, destFileOption, columnsOption);

    return await rootCommand.InvokeAsync(args);
    }

    private static void RemoveColumnsFromRecords(IList<dynamic> records, string[] columnsToRemove)
    {
        foreach (var record in records)
        {
            var row = (IDictionary<string, object>) record;
            foreach (var columnName in columnsToRemove)
            {
                var isSuccessful = row.Remove(columnName);
                if (!isSuccessful)
                {
                    Console.WriteLine($"Failed to remove column: {columnName}");
                }
            }
        }
    }

    private static IList<dynamic> ReadCsvAsRecords(FileInfo csvFile)
    {
        using (var reader = new StreamReader(csvFile.FullName))
        {
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<dynamic>().ToList();
            }
        }
    }

    private static void AppendMockColumns(IList<dynamic> csvRecords)
    {
        Randomizer.Seed = new Random(1233212);
        var faker = new Faker("en");
        foreach (var record in csvRecords)
        {
            record.FirstName = faker.Name.FirstName();
            record.LastName = faker.Name.LastName();
            record.Address = faker.Address.FullAddress();
            record.Country = faker.Address.CountryCode();
            record.ZipCode = faker.Address.ZipCode();
        }
    }

    private static void OverrideFileWithNewRecords(IList<dynamic> csvRecords, FileInfo destCsvFileInfo)
    {
        using (var stream = File.Open(destCsvFileInfo.FullName, FileMode.Create))
        {
            using (var writer = new StreamWriter(stream))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(csvRecords);
                }
            }
        }
    }
}