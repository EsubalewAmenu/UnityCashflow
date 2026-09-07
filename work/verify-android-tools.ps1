param([string[]]$Packages=@("ndk.zip","tools.zip","build-tools.zip","platform.zip"))
$ErrorActionPreference="Stop"
$download=Join-Path $PSScriptRoot "sdk-downloads"
$out=Join-Path $download "ndk.zip"
if($Packages -contains "ndk.zip"){
$stream=[IO.File]::Create($out)
try {
 for($i=0;$i -lt 8;$i++){
  foreach($suffix in @("part","tail")){
   $file=Join-Path $download "ndk-$i.$suffix"
   if(Test-Path $file){$inputStream=[IO.File]::OpenRead($file);try{$inputStream.CopyTo($stream)}finally{$inputStream.Dispose()}}
  }
 }
} finally {$stream.Dispose()}
}
$expected=@{"ndk.zip"="6e3fb50022c611a2b13d02f5de5c21cc7206a298";"tools.zip"="3098050fb2bdc7cc8a07bdef4dd13035f9f78d2a";"build-tools.zip"="62cfde1b6fcc3ad12a4d2ba1b537e752768bfd47";"platform.zip"="1f2e9478d6a7601425ceaa553311dc43191f103d"}
foreach($name in $Packages){if((Get-FileHash (Join-Path $download $name) -Algorithm SHA1).Hash -ne $expected[$name]){throw "Checksum mismatch: $name"}}
$stage=Join-Path $download "unpacked"
foreach($name in $Packages){
 $destination=Join-Path $stage $name.Replace(".zip","")
 New-Item -ItemType Directory -Path $destination -Force | Out-Null
 & tar.exe -xf (Join-Path $download $name) -C $destination
 if($LASTEXITCODE -ne 0){throw "Extraction failed: $name"}
 Write-Output "Verified and extracted $name"
}
Write-Output "Verified requested Google archives and extracted to $stage"
