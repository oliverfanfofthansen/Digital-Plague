param(
    [string]$SourceFile = 'Temp/natural-earth-countries.geojson',
    [string]$OutputFile = 'Assets/DigitalPlague/Data/WorldCountries.json'
)

# Run from the project root. Download source separately; never execute remote code.
# Source: Natural Earth ne_110m_admin_0_countries.geojson (public domain).
$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath $SourceFile -Raw | ConvertFrom-Json
$countries = foreach ($feature in $source.features) {
    $rings = [System.Collections.Generic.List[object]]::new()
    $polygons = [System.Collections.Generic.List[object]]::new()
    if ($feature.geometry.type -eq 'Polygon') {
        $polygons.Add($feature.geometry.coordinates)
    } else {
        foreach ($polygon in $feature.geometry.coordinates) { $polygons.Add($polygon) }
    }
    foreach ($polygon in $polygons) {
        foreach ($ring in $polygon) {
            $points = foreach ($point in $ring) {
                @{ x = [math]::Round([double]$point[0], 4); y = [math]::Round([double]$point[1], 4) }
            }
            $rings.Add(@{ points = @($points) })
        }
    }
    @{ code = $feature.properties.ADM0_A3; name = $feature.properties.ADMIN; rings = @($rings.ToArray()) }
}
$json = @{ countries = @($countries) } | ConvertTo-Json -Depth 20 -Compress
[System.IO.File]::WriteAllText((Join-Path (Get-Location) $OutputFile), $json)
Write-Output "Imported $($countries.Count) country shapes into $OutputFile"
