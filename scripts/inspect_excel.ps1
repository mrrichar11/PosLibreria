try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false

    $files = @(
        'Lista de precios ALMA LIBRE.xlsx',
        'lista de precio proveedor El Once_2026-09-17 (Con Cod.Barra).xls',
        'lista de precio proveedor El Once_2026-09-17 (Con Cod.Producto).xls'
    )

    foreach ($f in $files) {
        $path = Join-Path $PSScriptRoot "..\$f"
        if (Test-Path $path) {
            $wb = $excel.Workbooks.Open($path)
            $sheet = $wb.Sheets.Item(1)
            Write-Host "========================================"
            Write-Host "FILE: $f"
            Write-Host "Sheet Name: $($sheet.Name)"
            Write-Host "Total Rows: $($sheet.UsedRange.Rows.Count)"
            $cols = @()
            for ($c = 1; $c -le 20; $c++) {
                $header = $sheet.Cells.Item(1, $c).Text
                if (-not [string]::IsNullOrWhiteSpace($header)) {
                    $cols += "[$c] $header"
                }
            }
            Write-Host "Headers: $($cols -join ', ')"

            # Print first 3 rows
            for ($r = 2; $r -le 4; $r++) {
                $vals = @()
                for ($c = 1; $c -le $cols.Count; $c++) {
                    $vals += $sheet.Cells.Item($r, $c).Text
                }
                Write-Host "Row $($r): $($vals -join ' | ')"
            }

            if ($f -like "*ALMA LIBRE*") {
                $rubroCount = 0
                for ($r = 2; $r -le $sheet.UsedRange.Rows.Count; $r++) {
                    $val = $sheet.Cells.Item($r, 12).Text
                    if (-not [string]::IsNullOrWhiteSpace($val)) { $rubroCount++ }
                }
                Write-Host "Total non-empty rubros: $rubroCount"
            }
            $wb.Close($false)
        }
    }
    $excel.Quit()
} catch {
    Write-Error $_
}
