$csvFilePath = ""

$maxFileSizeMB = 50

# Read the CSV file into a variable
$data = Import-Csv $csvFilePath
$measuredData = ($data | Measure-Object)
# Calculate the number of files to be created
$estimatedNumFiles = [Math]::Ceiling((Get-Item $csvFilePath).length / ($maxFileSizeMB * 1024 * 1024))
$maxRowsPerFile = [Math]::Floor($measuredData.Count / $estimatedNumFiles)

# Loop through the rows and write them to separate CSV files
$currentRow = 0
$currentFileIndex = 1
while ($currentFileIndex -le $estimatedNumFiles)
{
    # Create a new file name for the current split file
    $currentFileName = "{0}_{1}.csv" -f ($csvFilePath -replace '\.csv$', ''), $currentFileIndex

    # Write the current batch of rows to the new file
    $data[$currentRow..($currentRow + $maxRowsPerFile - 1)] | Export-Csv $currentFileName -NoTypeInformation

    # Update the current row and file index counters
    $currentRow += $maxRowsPerFile
    $currentFileIndex++
}

# Display completion message
Write-Host "Splitting complete. ($currentFileIndex-1) files created."
