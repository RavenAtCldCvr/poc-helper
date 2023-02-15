# Set the path to your CSV file
$csvPath = ""

# Set the names of the columns you want to remove
$columnsToRemove = @("firstname", "lastname", "b_firstname", "b_lastname", "b_address","b_address_2", "b_city","b_county", "b_state",
"b_country", "b_zipcode", "b_phone", "s_firstname", "s_lastname", "s_address", "s_address_2", "s_city", "s_county", "s_state", "s_country", "s_zipcode",
"s_phone", "s_address_type","ip_address","odata_etag", "last_api_log")

# Load the CSV file
$csv = Import-Csv $csvPath

# Loop through each row and column, replacing new lines with whitespace
foreach ($row in $data) {
    foreach ($col in $row) {
        $col = $col -replace "`n", " "
    }
}

$newCsvPath = "{0}_{1}.csv" -f ($csvPath -replace '\.csv$', ''), 'short'
# Remove the specified columns from the CSV
$csv = $csv | Select-Object * -ExcludeProperty odata_etag,last_api_log

# Save the modified CSV file
$csv | Export-Csv $newCsvPath -NoTypeInformation