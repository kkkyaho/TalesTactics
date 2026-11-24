param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Image]::FromFile($Path)
$meta=[IO.File]::ReadAllText($Path+'.meta')
$matches=[regex]::Matches($meta,'name: ([^\r\n]+)\s+rect:\s+serializedVersion: \d+\s+x: ([\d.]+)\s+y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+)')
if($matches.Count -lt 4){throw 'Need four reference sprites'}
$bmp=[Drawing.Bitmap]::new(720,720)
$g=[Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::FromArgb(65,70,80))
for($i=0;$i -lt 4;$i++) {
 $m=$matches[$i];$x=[int]$m.Groups[2].Value;$y=[int]$m.Groups[3].Value;$w=[int]$m.Groups[4].Value;$h=[int]$m.Groups[5].Value
 $scale=[Math]::Min(330.0/$w,330.0/$h);$dw=[int]($w*$scale);$dh=[int]($h*$scale)
 $dx=[int](($i%2)*360+(360-$dw)/2);$dy=[int]([Math]::Floor($i/2)*360+345-$dh)
 $g.DrawImage($im,[Drawing.Rectangle]::new($dx,$dy,$dw,$dh),[Drawing.Rectangle]::new($x,$im.Height-$y-$h,$w,$h),[Drawing.GraphicsUnit]::Pixel)
}
$stream=[IO.MemoryStream]::new();$bmp.Save($stream,[Drawing.Imaging.ImageFormat]::Jpeg)
[Convert]::ToBase64String($stream.ToArray())
$stream.Dispose();$g.Dispose();$bmp.Dispose();$im.Dispose()
