# Vérifier l'intégrité du projet
Write-Host "=== Vérification du projet ===" -ForegroundColor Yellow

# 1. Lister tous les fichiers DLL
Write-Host "`n1. Fichiers DLL trouvés:" -ForegroundColor Cyan
Get-ChildItem -Path . -Filter *.dll -Recurse | ForEach-Object {
    Write-Host "  - $($_.FullName)"
}

# 2. Vérifier les packages
Write-Host "`n2. Packages installés:" -ForegroundColor Cyan
dotnet list package --include-transitive

# 3. Calculer les hash des fichiers sources
Write-Host "`n3. Hash des fichiers sources:" -ForegroundColor Cyan
Get-ChildItem -Path . -Include *.cs,*.xaml -Recurse | ForEach-Object {
    $hash = Get-FileHash $_.FullName -Algorithm SHA256
    Write-Host "  $($_.Name): $($hash.Hash.Substring(0,16))..."
}

# 4. Rechercher des patterns suspects
Write-Host "`n4. Recherche de patterns suspects:" -ForegroundColor Cyan
$suspiciousPatterns = @(
    "DownloadFile",
    "WebClient",
    "Process.Start",
    "Assembly.Load",
    "DllImport",
    "unsafe",
    "Marshal",
    "trojan",
    "malware",
    "hack",
    "crack"
)

foreach ($pattern in $suspiciousPatterns) {
    $found = Get-ChildItem -Path . -Include *.cs -Recurse | Select-String -Pattern $pattern
    if ($found) {
        Write-Host "  ⚠️ Pattern '$pattern' trouvé dans:" -ForegroundColor Red
        $found | ForEach-Object { Write-Host "    - $($_.Filename):$($_.LineNumber)" }
    }
}

Write-Host "`n=== Fin de la vérification ===" -ForegroundColor Green