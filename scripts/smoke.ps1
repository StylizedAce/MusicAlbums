[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5080",
    [string]$Provider = "deezer",
    [string]$ProviderAlbumId = "302127",
    [string]$Query = "daft punk"
)

$ErrorActionPreference = "Stop"

Write-Host "Smoke testing $BaseUrl (provider=$Provider, album=$ProviderAlbumId)"

$providers = Invoke-RestMethod "$BaseUrl/api/providers"
if (-not ($providers | Where-Object { $_.name -eq $Provider })) {
    throw "Provider '$Provider' is not registered."
}
Write-Host "[ok] providers: $(($providers | ForEach-Object { $_.name }) -join ', ')"

$search = Invoke-RestMethod "$BaseUrl/api/albums/search?q=$([uri]::EscapeDataString($Query))&provider=$Provider&limit=3"
if (@($search.albums).Count -lt 1) {
    throw "Search for '$Query' returned no albums."
}
Write-Host "[ok] search '$Query' -> total=$($search.total), first='$(@($search.albums)[0].title)'"

$user = "smoke-$([guid]::NewGuid().ToString('N'))"
$saveBody = @{ provider = $Provider; providerAlbumId = $ProviderAlbumId } | ConvertTo-Json

$save = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/users/$user/library" -ContentType "application/json" -Body $saveBody
if (-not $save.id) {
    throw "Save did not return an album id."
}
Write-Host "[ok] saved '$($save.title)' with $($save.tracks.Count) tracks for $user"

$library = Invoke-RestMethod "$BaseUrl/api/users/$user/library"
if (@($library).Count -ne 1) {
    throw "Library should contain exactly one album."
}
Write-Host "[ok] library contains '$(@($library)[0].title)'"

$duplicateStatus = 0
try {
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/users/$user/library" -ContentType "application/json" -Body $saveBody | Out-Null
} catch {
    $duplicateStatus = [int]$_.Exception.Response.StatusCode
}
if ($duplicateStatus -ne 409) {
    throw "Duplicate save should return 409 but returned $duplicateStatus."
}
Write-Host "[ok] duplicate save -> 409"

Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/users/$user/library/$($save.id)" | Out-Null
$after = Invoke-RestMethod "$BaseUrl/api/users/$user/library"
if ($after) {
    throw "Library should be empty after delete."
}
Write-Host "[ok] delete -> library empty"

Write-Host "SMOKE TEST PASSED ($BaseUrl)"
