$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
try {
    $wb = $excel.Workbooks.Open("C:\Proyectos\PuntoDeVentaLibreria\Lista de precios ALMA LIBRE.xlsx")
    $sheet = $wb.Sheets.Item(1)
    $maxCol = $sheet.UsedRange.Columns.Count
    Write-Host "Used Range Columns: $maxCol"
    $headers = @()
    for ($c = 1; $c -le $maxCol; $c++) {
        $h = $sheet.Cells.Item(1, $c).Text
        $headers += "[$c] $h"
    }
    Write-Host "Headers:"
    Write-Host ($headers -join "`n")

    Write-Host "`n--- Sample Row 2 values ---"
    for ($c = 1; $c -le $maxCol; $c++) {
        $h = $sheet.Cells.Item(1, $c).Text
        $v = $sheet.Cells.Item(2, $c).Text
        Write-Host "[$c] $h = '$v'"
    }

    $wb.Close($false)
} finally {
    $excel.Quit()
}
