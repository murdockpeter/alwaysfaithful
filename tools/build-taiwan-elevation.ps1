param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\AlwaysFaithful\Assets\Resources\Geography')
)

$ErrorActionPreference = 'Stop'
$culture = [System.Globalization.CultureInfo]::InvariantCulture
$sourceUrl = 'https://coastwatch.pfeg.noaa.gov/erddap/griddap/ETOPO_2022_v1_15s.csv0?z[(21.7):8:(25.4)][(119.75):8:(122.2)]'
$response = Invoke-WebRequest -Uri $sourceUrl -UseBasicParsing -TimeoutSec 180
$lines = $response.Content -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
if ($lines.Count -lt 4) { throw 'NOAA ETOPO response did not contain a usable elevation grid.' }

$latitudes = [System.Collections.Generic.List[double]]::new()
$longitudes = [System.Collections.Generic.List[double]]::new()
$elevations = [System.Collections.Generic.List[int16]]::new()
foreach ($line in $lines) {
    $fields = $line.Trim().Split(',')
    if ($fields.Count -ne 3) { throw "Unexpected ETOPO row: $line" }
    $latitudes.Add([double]::Parse($fields[0], $culture))
    $longitudes.Add([double]::Parse($fields[1], $culture))
    $metres = [Math]::Round([double]::Parse($fields[2], $culture))
    $elevations.Add([int16][Math]::Min([int16]::MaxValue, [Math]::Max([int16]::MinValue, $metres)))
}

$firstLatitude = $latitudes[0]
$width = 0
while ($width -lt $latitudes.Count -and [Math]::Abs($latitudes[$width] - $firstLatitude) -lt 0.0000001) { $width++ }
if ($width -lt 2 -or $latitudes.Count % $width -ne 0) { throw 'ETOPO rows do not form a regular rectangular grid.' }
$height = [int]($latitudes.Count / $width)

[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$binaryPath = Join-Path $OutputDirectory 'taiwan-etopo-2022.bytes'
$stream = [IO.File]::Open($binaryPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
$writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([Text.Encoding]::ASCII.GetBytes('SOUELEV1'))
    $writer.Write([int]$width)
    $writer.Write([int]$height)
    $writer.Write([double]$longitudes[0])
    $writer.Write([double]$longitudes[$width - 1])
    $writer.Write([double]$latitudes[0])
    $writer.Write([double]$latitudes[($height - 1) * $width])
    foreach ($elevation in $elevations) { $writer.Write([int16]$elevation) }
}
finally {
    $writer.Dispose()
    $stream.Dispose()
}

$metadata = [ordered]@{
    title = 'Taiwan ETOPO 2022 two-arc-minute elevation subset'
    source = 'NOAA National Centers for Environmental Information, ETOPO 2022 15 Arc-Second Global Relief Model'
    doi = '10.25921/fd45-gt74'
    sourceUrl = $sourceUrl
    accessedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-dd')
    stride = 8
    sourceResolutionArcSeconds = 15
    bakedResolutionArcSeconds = 120
    width = $width
    height = $height
    west = $longitudes[0]
    east = $longitudes[$width - 1]
    south = $latitudes[0]
    north = $latitudes[($height - 1) * $width]
    sampleCount = $elevations.Count
    license = 'United States government work; source attribution retained. Not for navigation.'
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'taiwan-etopo-2022-metadata.json'), ($metadata | ConvertTo-Json -Depth 3) + "`n", [Text.UTF8Encoding]::new($false))
Write-Output "Baked $($elevations.Count) ETOPO samples ($width x $height) to $binaryPath"
