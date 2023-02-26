# Set the path to your CSV file
$csvPath = ""

# Set the names of the columns you want to remove
$columnsToRemove = @("lastname", "b_lastname", "b_address", "b_address_2", "b_phone", "s_firstname", "s_lastname", "s_address", "s_address_2",
"s_phone", "s_address_type", "ip_address", "odata_etag", "last_api_log", "payment_surcharge", "shipping_ids", "shipping_cost",
"status", "notes", "details", "promotions", "promotion_ids", "company", "phone", "fax", "url", "email", "payment_id", "tax_exempt", "repaid", "validation_code", "localization_id",
"profile_id", "storefront_id", "updated_at", "weight", "document_no", "sd_ga_cid", "sd_ga_status", "retry_count", "retry_after_timestamp", "outstanding_amount", "bc_status", "is_mobile_order")

# Load the CSV file
$csv = Import-Csv $csvPath

# Loop through each row and column, replacing new lines with whitespace
#foreach ($row in $data) {
#    foreach ($col in $row) {
#        $col = $col -replace "`n", " "
#    }
#}

$newCsvPath = "{0}_{1}.csv" -f ($csvPath -replace '\.csv$', ''), 'max_with_vendor'
# Remove the specified columns from the CSV
$csv = $csv | Select-Object * -ExcludeProperty $columnsToRemove

# Save the modified CSV file
$csv | Export-Csv $newCsvPath -NoTypeInformation
