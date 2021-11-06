$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url64      = 'https://github.com/Ruben2776/PicView/releases/download/1.5.6/PicView-v1.5.6.exe' # 64bit URL here (HTTPS preferred) or remove - if installer contains both (very rare), use $url

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'EXE'
  url64bit      = $url64
  softwareName  = 'Picview version 1.5.6*'
  checksum64    = '8a6958368ed94c32b7736d86cdb1f8dae9154c0595b590b284a5eafeaf8ccb14'
  checksumType64= 'sha256' 
  silentArgs   = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-' 
}

Install-ChocolateyPackage @packageArgs
