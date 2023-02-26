using System.CommandLine;
using System.Globalization;
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

        return await rootCommand.InvokeAsync(args);
    }

    private static void RemoveColumnsFromRecords(IList<dynamic> records, string[] columnsToRemove)
    {
        if (records == null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        if (columnsToRemove.Length == 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(columnsToRemove));
        }

        foreach (dynamic record in records)
        {
            var row = (IDictionary<string, object>)record;
            foreach (string columnName in columnsToRemove)
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
