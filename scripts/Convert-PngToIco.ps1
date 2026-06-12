param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePng,

    [Parameter(Mandatory = $true)]
    [string]$OutputIco
)

Add-Type -AssemblyName System.Drawing

$sizes = @(256, 128, 64, 48, 32, 16)
$source = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $SourcePng))
$pngStreams = New-Object System.Collections.Generic.List[byte[]]

try {
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

        try {
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.DrawImage($source, 0, 0, $size, $size)

            $memory = New-Object System.IO.MemoryStream
            $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngStreams.Add($memory.ToArray())
            $memory.Dispose()
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
}
finally {
    $source.Dispose()
}

$outputPath = [System.IO.Path]::GetFullPath($OutputIco)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputPath)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$file = [System.IO.File]::Open($outputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = New-Object System.IO.BinaryWriter $file

try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$pngStreams.Count)

    $offset = 6 + (16 * $pngStreams.Count)
    for ($i = 0; $i -lt $pngStreams.Count; $i++) {
        $size = $sizes[$i]
        $data = $pngStreams[$i]

        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$data.Length)
        $writer.Write([UInt32]$offset)

        $offset += $data.Length
    }

    foreach ($data in $pngStreams) {
        $writer.Write($data)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}
