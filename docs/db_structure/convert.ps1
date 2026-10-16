$root = ".\"

$utf8 = [System.Text.UTF8Encoding]::new($false)
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)

Get-ChildItem -Path $root -Filter "*.sql" -File -Recurse | ForEach-Object {
    $file = $_.FullName
    $bytes = [System.IO.File]::ReadAllBytes($file)

    if ($bytes.Length -eq 0) {
        return
    }

    # UTF-16 LE BOM
    if ($bytes.Length -ge 2 -and
        $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {

        $encoding = [System.Text.Encoding]::Unicode
        $text = $encoding.GetString($bytes, 2, $bytes.Length - 2)
    }
    # UTF-16 BE BOM
    elseif ($bytes.Length -ge 2 -and
        $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {

        $encoding = [System.Text.Encoding]::BigEndianUnicode
        $text = $encoding.GetString($bytes, 2, $bytes.Length - 2)
    }
    # UTF-8 BOM
    elseif ($bytes.Length -ge 3 -and
        $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and
        $bytes[2] -eq 0xBF) {

        $text = $utf8Strict.GetString($bytes, 3, $bytes.Length - 3)
    }
    # UTF-8 без BOM
    else {
        try {
            $text = $utf8Strict.GetString($bytes)
        }
        catch {
            Write-Warning "Unknown encoding, skipped: $file"
            return
        }
    }

    [System.IO.File]::WriteAllText($file, $text, $utf8)
    Write-Host "Converted: $file"
}