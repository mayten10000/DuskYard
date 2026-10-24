Add-Type -AssemblyName System.Windows.Forms.DataVisualization
$auditFolder = Join-Path $PSScriptRoot 'Verification\MemoryAudit'
$samples = Import-Csv -LiteralPath (Join-Path $auditFolder 'memory-samples.csv')
$chart = New-Object System.Windows.Forms.DataVisualization.Charting.Chart
$chart.Width = 1200
$chart.Height = 520
$chart.BackColor = [System.Drawing.Color]::White
$area = New-Object System.Windows.Forms.DataVisualization.Charting.ChartArea 'Memory'
$area.AxisX.Title = 'Stress time (seconds)'
$area.AxisY.Title = 'Unity allocated memory (MiB)'
$area.AxisX.Minimum = 0
$area.AxisX.Maximum = 180
$area.AxisX.MajorGrid.LineColor = [System.Drawing.Color]::Gainsboro
$area.AxisY.MajorGrid.LineColor = [System.Drawing.Color]::Gainsboro
$area.AxisY.IsStartedFromZero = $false
$area.AxisY.LabelStyle.Format = 'F2'
$chart.ChartAreas.Add($area)
$series = New-Object System.Windows.Forms.DataVisualization.Charting.Series 'Allocated'
$series.ChartType = 'Line'
$series.BorderWidth = 3
$series.Color = [System.Drawing.Color]::FromArgb(64, 107, 139)
foreach ($sample in $samples) {
    $xValue = [double]::Parse($sample.stress_seconds, [Globalization.CultureInfo]::InvariantCulture)
    $yValue = [double]::Parse($sample.allocated_bytes, [Globalization.CultureInfo]::InvariantCulture) / 1048576
    $series.Points.AddXY($xValue, $yValue) | Out-Null
}
$chart.Series.Add($series)
$chart.Titles.Add('Standalone memory audit - 180 seconds') | Out-Null
$chart.SaveImage((Join-Path $auditFolder 'memory-trend.png'), 'Png')
$chart.Dispose()
