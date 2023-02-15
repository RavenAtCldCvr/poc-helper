# Set the path to your CSV file
$csvFilePath = "C:\path\to\large\file.csv"

# Set the maximum file size (in megabytes) for each split file
$maxFileSizeMB = 150

# Calculate the maximum number of rows per file based on the maximum file size
$maxRowsPerFile = $maxFileSizeMB * 1024 * 1024 / (Get-Item $csvFilePath).length

# Read the CSV file into a variable
$data = Import-Csv $csvFilePath

# Loop through the rows and write them to separate CSV files
$currentRow = 0
$currentFileIndex = 1
while ($currentRow -lt $data.Count) {
    # Create a new file name for the current split file
    $currentFileName = "{0}_{1}.csv" -f ($csvFilePath -replace '\.csv$', ''), $currentFileIndex
    
    # Write the current batch of rows to the new file
    $data[$currentRow..($currentRow + $maxRowsPerFile - 1)] | Export-Csv $currentFileName -NoTypeInformation
    
    # Update the current row and file index counters
    $currentRow += $maxRowsPerFile
    $currentFileIndex++
}