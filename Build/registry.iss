[Registry]
; Register software
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "PicView"; ValueData: "Software\Clients\StartMenuInternet\PicView\Capabilities"; Flags: uninsdeletekey
; Open with PicView
Root: HKCR; Subkey: "*\shell\PicView"; ValueType: string; ValueName: ""; ValueData: "Open with {#MyAppName}"; Flags: uninsdeletekey; Tasks: "openwith"
Root: HKCR; Subkey: "*\shell\PicView"; ValueType: string; ValueName: "Icon"; ValueData: """{#AppIcon}"""; Flags: uninsdeletekey; Tasks: "openwith"
Root: HKCR; Subkey: "*\shell\PicView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%V"""; Flags: uninsdeletekey; Tasks: "openwith"

; Browse folder with PicView
Root: HKCR; Subkey: "Directory\Background\shell\PicView"; ValueType: string; ValueName: ""; ValueData: "Browse folder with {#MyAppName}"; Flags: uninsdeletekey; Tasks: "browsefolder"
Root: HKCR; Subkey: "Directory\Background\shell\PicView"; ValueType: string; ValueName: "Icon"; ValueData: """{#AppIcon}"""; Flags: uninsdeletekey; Tasks: "browsefolder"
Root: HKCR; Subkey: "Directory\Background\shell\PicView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%V"""; Flags: uninsdeletekey; Tasks: "browsefolder"
Root: HKCR; Subkey: "Directory\shell\PicView"; ValueType: string; ValueName: ""; ValueData: "Browse folder with {#MyAppName}"; Flags: uninsdeletekey; Tasks: "browsefolder"
Root: HKCR; Subkey: "Directory\shell\PicView"; ValueType: string; ValueName: "Icon"; ValueData: """{#AppIcon}"""; Flags: uninsdeletekey; Tasks: "browsefolder"
Root: HKCR; Subkey: "Directory\shell\PicView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%V"""; Flags: uninsdeletekey; Tasks: "browsefolder"

; JPG
Root: HKCR; Subkey: ".jpg"; ValueType: string; ValueName: ""; ValueData: "JpgImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "JpgImg"; ValueType: string; ValueName: ""; ValueData: "JPG image (Joint photographic experts group)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "JpgImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; JPE
Root: HKCR; Subkey: ".jpe"; ValueType: string; ValueName: ""; ValueData: "JpeImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "JpeImg"; ValueType: string; ValueName: ""; ValueData: "JPE image (Joint photographic experts group)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "JpeImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; JPEG
Root: HKCR; Subkey: ".jpeg"; ValueType: string; ValueName: ""; ValueData: "JpegImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "JpegImg"; ValueType: string; ValueName: ""; ValueData: "JPEG image (Joint photographic experts group)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "JpegImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; PNG
Root: HKCR; Subkey: ".png"; ValueType: string; ValueName: ""; ValueData: "PngImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PngImg"; ValueType: string; ValueName: ""; ValueData: "PNG image (Portable network graphics)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PngImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; BMP
Root: HKCR; Subkey: ".bmp"; ValueType: string; ValueName: ""; ValueData: "BmpImg"; Flags: uninsdeletekey
Root: HKCR; Subkey: "BmpImg"; ValueType: string; ValueName: ""; ValueData: "BMP image (Bitmap image file)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "BmpImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; GIF
Root: HKCR; Subkey: ".gif"; ValueType: string; ValueName: ""; ValueData: "GifImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "GifImg"; ValueType: string; ValueName: ""; ValueData: "GIF image (Graphics interchange format)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "GifImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; JFIF
Root: HKCR; Subkey: ".jfif"; ValueType: string; ValueName: ""; ValueData: "JfifImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "JfifImg"; ValueType: string; ValueName: ""; ValueData: "JFIF image (JPEG file interchange format)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "JfifImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; ICO
Root: HKCR; Subkey: ".ico"; ValueType: string; ValueName: ""; ValueData: "ico"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "DdsTexture"; ValueType: string; ValueName: ""; ValueData: "Icon"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "DdsTexture\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; WEBP
Root: HKCR; Subkey: ".webp"; ValueType: string; ValueName: ""; ValueData: "WebpImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "WebpImg"; ValueType: string; ValueName: ""; ValueData: "WEBP image (Google web picture)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "WebpImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; WBMP
Root: HKCR; Subkey: ".wbmp"; ValueType: string; ValueName: ""; ValueData: "WbmpImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "WebpImg"; ValueType: string; ValueName: ""; ValueData: "Wireless Application Protocol Bitmap Format"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "WebpImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""



; PSD
Root: HKCR; Subkey: ".psd"; ValueType: string; ValueName: ""; ValueData: "PsdImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PsdImg"; ValueType: string; ValueName: ""; ValueData: "PSD file (Photoshop Document)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PsdImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; PSB
Root: HKCR; Subkey: ".psb"; ValueType: string; ValueName: ""; ValueData: "PsbImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PsdImg"; ValueType: string; ValueName: ""; ValueData: "Adobe Photoshop Large Document file"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PsdImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""



; TIF
Root: HKCR; Subkey: ".tif"; ValueType: string; ValueName: ""; ValueData: "TifImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "TifImg"; ValueType: string; ValueName: ""; ValueData: "TIF image (Tagged image file format)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "TifImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; TIFF
Root: HKCR; Subkey: ".tiff"; ValueType: string; ValueName: ""; ValueData: "TiffImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "TiffImg"; ValueType: string; ValueName: ""; ValueData: "TIFF image (Tagged image file format)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "TiffImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; DDS
Root: HKCR; Subkey: ".dds"; ValueType: string; ValueName: ""; ValueData: "DdsTexture"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "DdsTexture"; ValueType: string; ValueName: ""; ValueData: "DDS texture (DirectDraw surface)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "DdsTexture\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; TGA
Root: HKCR; Subkey: ".tga"; ValueType: string; ValueName: ""; ValueData: "TgaTexture"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "TgaTexture"; ValueType: string; ValueName: ""; ValueData: "TGA texture (Truevision advanced raster graphics)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "TgaTexture\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; HEIC
Root: HKCR; Subkey: ".heic"; ValueType: string; ValueName: ""; ValueData: "HeicImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "heicImg"; ValueType: string; ValueName: ""; ValueData: "High Efficiency Image File Format"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "HeicImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; HEIF
Root: HKCR; Subkey: ".heif"; ValueType: string; ValueName: ""; ValueData: "HeifImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "HeifImg"; ValueType: string; ValueName: ""; ValueData: "High Efficiency Image File Format"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "HeifImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; HDR
Root: HKCR; Subkey: ".hdr"; ValueType: string; ValueName: ""; ValueData: "HdrImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "HdrImg"; ValueType: string; ValueName: ""; ValueData: "High Dynamic Range Image file"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "HdrImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""


; AVIF
Root: HKCR; Subkey: ".avif"; ValueType: string; ValueName: ""; ValueData: "AvifImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "AvifImg"; ValueType: string; ValueName: ""; ValueData: "AV1 Image File Format"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "AvifImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""



; SVG
Root: HKCR; Subkey: ".svg"; ValueType: string; ValueName: ""; ValueData: "SvgImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "SvgImg"; ValueType: string; ValueName: ""; ValueData: "SVG image (Scalable vector graphics)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "SvgImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; xcf
Root: HKCR; Subkey: ".xcf"; ValueType: string; ValueName: ""; ValueData: "xcfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "XcfImg"; ValueType: string; ValueName: ""; ValueData: "eXperimental Computing Facility"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "XcfImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""




; 3fr
Root: HKCR; Subkey: ".3fr"; ValueType: string; ValueName: ""; ValueData: "3frImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "3frImg"; ValueType: string; ValueName: ""; ValueData: "Hasselblad 3F RAW Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "3frImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; arw
Root: HKCR; Subkey: ".arw"; ValueType: string; ValueName: ""; ValueData: "ArwImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "ArwImg"; ValueType: string; ValueName: ""; ValueData: "Sony Alpha Raw Digital Camera Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "ArwImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; CR2
Root: HKCR; Subkey: ".cr2"; ValueType: string; ValueName: ""; ValueData: "Cr2Img"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "Cr2Img"; ValueType: string; ValueName: ""; ValueData: "CR2 image (Canon Digital Camera Raw)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "Cr2Img\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; CR3
Root: HKCR; Subkey: ".cr3"; ValueType: string; ValueName: ""; ValueData: "Cr3Img"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "Cr3Img"; ValueType: string; ValueName: ""; ValueData: "CR3 image (Canon Digital Camera Raw)"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "Cr3Img\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; CRW
Root: HKCR; Subkey: ".crw"; ValueType: string; ValueName: ""; ValueData: "CrwImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "CrwImg"; ValueType: string; ValueName: ""; ValueData: "Canon Raw CIFF Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "CrWImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; DCR
Root: HKCR; Subkey: ".dcr"; ValueType: string; ValueName: ""; ValueData: "DcrImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "DcrImg"; ValueType: string; ValueName: ""; ValueData: "Kodak Digital Camera RAW Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "DcrImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; DNG
Root: HKCR; Subkey: ".dng"; ValueType: string; ValueName: ""; ValueData: "DngImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "DngImg"; ValueType: string; ValueName: ""; ValueData: "Digital Negative Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "DngImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; ERF
Root: HKCR; Subkey: ".erf"; ValueType: string; ValueName: ""; ValueData: "ErfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "ErfImg"; ValueType: string; ValueName: ""; ValueData: "Epson RAW File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "ErfImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; KDC
Root: HKCR; Subkey: ".kdc"; ValueType: string; ValueName: ""; ValueData: "KdcImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "KdcImg"; ValueType: string; ValueName: ""; ValueData: "Kodak Photo-Enhancer File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "KdcImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; MEF
Root: HKCR; Subkey: ".mef"; ValueType: string; ValueName: ""; ValueData: "MefImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "MefImg"; ValueType: string; ValueName: ""; ValueData: "Mamiya RAW Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "MefImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; MOS
Root: HKCR; Subkey: ".mos"; ValueType: string; ValueName: ""; ValueData: "MosImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "MosImg"; ValueType: string; ValueName: ""; ValueData: "Leaf Camera RAW File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "MosImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; MRW
Root: HKCR; Subkey: ".mrw"; ValueType: string; ValueName: ""; ValueData: "MrwImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "MrwImg"; ValueType: string; ValueName: ""; ValueData: "Minolta Raw Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "MrwImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; NEF
Root: HKCR; Subkey: ".nef"; ValueType: string; ValueName: ""; ValueData: "NefImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "NefImg"; ValueType: string; ValueName: ""; ValueData: "Nikon Electronic Format RAW Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "NefImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; NRW
Root: HKCR; Subkey: ".nrw"; ValueType: string; ValueName: ""; ValueData: "NrwImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "NrwImg"; ValueType: string; ValueName: ""; ValueData: "Nikon Raw Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "NrwImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; ORF
Root: HKCR; Subkey: ".orf"; ValueType: string; ValueName: ""; ValueData: "OrfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "OrfImg"; ValueType: string; ValueName: ""; ValueData: "Olympus RAW File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "OrwImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; PEF
Root: HKCR; Subkey: ".pef"; ValueType: string; ValueName: ""; ValueData: "PefImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PefImg"; ValueType: string; ValueName: ""; ValueData: "Pentax Electronic File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PefImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; RAF
Root: HKCR; Subkey: ".raf"; ValueType: string; ValueName: ""; ValueData: "RafImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "RafImg"; ValueType: string; ValueName: ""; ValueData: "Fuji RAW Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "RafImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; RAW
Root: HKCR; Subkey: ".raw"; ValueType: string; ValueName: ""; ValueData: "RawImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "RawImg"; ValueType: string; ValueName: ""; ValueData: "Raw Image Data File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "RawImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; RW2
Root: HKCR; Subkey: ".rw2"; ValueType: string; ValueName: ""; ValueData: "Rw2Img"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "Rw2Img"; ValueType: string; ValueName: ""; ValueData: "Panasonic RAW Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "Rw2Img\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; SRF
Root: HKCR; Subkey: ".srf"; ValueType: string; ValueName: ""; ValueData: "SrfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "SrfImg"; ValueType: string; ValueName: ""; ValueData: "Sony RAW Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "SrfImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; SRF
Root: HKCR; Subkey: ".srf"; ValueType: string; ValueName: ""; ValueData: "SrfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "SrfImg"; ValueType: string; ValueName: ""; ValueData: "Sony RAW Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "SrfImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; x3f
Root: HKCR; Subkey: ".x3f"; ValueType: string; ValueName: ""; ValueData: "X3fImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "X3fImg"; ValueType: string; ValueName: ""; ValueData: "SIGMA X3F Camera RAW File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "X3fImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; jp2
Root: HKCR; Subkey: ".jp2"; ValueType: string; ValueName: ""; ValueData: "jp2Img"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "jp2Img"; ValueType: string; ValueName: ""; ValueData: "JPEG 2000"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "jp2Img\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""




; pgm
Root: HKCR; Subkey: ".pgm"; ValueType: string; ValueName: ""; ValueData: "PgmImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PgmImg"; ValueType: string; ValueName: ""; ValueData: "Portable Gray Map Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PgmImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; ppm
Root: HKCR; Subkey: ".ppm"; ValueType: string; ValueName: ""; ValueData: "PpmImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PpmImg"; ValueType: string; ValueName: ""; ValueData: "Portable Pixmap Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PpmImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; cut
Root: HKCR; Subkey: ".cut"; ValueType: string; ValueName: ""; ValueData: "CutImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "CutImg"; ValueType: string; ValueName: ""; ValueData: "Dr. Halo Bitmap Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "CutImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""


; exr
Root: HKCR; Subkey: ".exr"; ValueType: string; ValueName: ""; ValueData: "ExrImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "ExrImg"; ValueType: string; ValueName: ""; ValueData: "OpenEXR Image"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "ExrImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; dib
Root: HKCR; Subkey: ".dib"; ValueType: string; ValueName: ""; ValueData: "DibImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "DibImg"; ValueType: string; ValueName: ""; ValueData: "Device Independent Bitmap File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "DibImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; emf
Root: HKCR; Subkey: ".emf"; ValueType: string; ValueName: ""; ValueData: "EmfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "EmfImg"; ValueType: string; ValueName: ""; ValueData: "Enhanced Windows Metafile"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "EmfImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; wbmp
Root: HKCR; Subkey: ".wbmp"; ValueType: string; ValueName: ""; ValueData: "WbmpImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "WbmpImg"; ValueType: string; ValueName: ""; ValueData: "WordPerfect Graphic File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "WbmpImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""


; wmf
Root: HKCR; Subkey: ".wmf"; ValueType: string; ValueName: ""; ValueData: "WmfImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "WmfImg"; ValueType: string; ValueName: ""; ValueData: "Windows Metafile"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "WmfImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; wpg
Root: HKCR; Subkey: ".wpg"; ValueType: string; ValueName: ""; ValueData: "WpgImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "WpgImg"; ValueType: string; ValueName: ""; ValueData: "WordPerfect Graphic File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "WpgImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; pcx
Root: HKCR; Subkey: ".pcx"; ValueType: string; ValueName: ""; ValueData: "PcxImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "PcxImg"; ValueType: string; ValueName: ""; ValueData: "Paintbrush Bitmap Image File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "PcxImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; xbm
Root: HKCR; Subkey: ".xbm"; ValueType: string; ValueName: ""; ValueData: "XbmImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "XbmImg"; ValueType: string; ValueName: ""; ValueData: "X11 Bitmap Graphic"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "XbmImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; xpm
Root: HKCR; Subkey: ".xpm"; ValueType: string; ValueName: ""; ValueData: "XpmImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "XpmImg"; ValueType: string; ValueName: ""; ValueData: "X11 Pixmap Graphic"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "XpmImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""


; b64
Root: HKCR; Subkey: ".b64"; ValueType: string; ValueName: ""; ValueData: "base64Img"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "base64Img"; ValueType: string; ValueName: ""; ValueData: "Base64"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "base64Img\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; thm
Root: HKCR; Subkey: ".thm"; ValueType: string; ValueName: "Video Thumbnail File"; ValueData: "thm"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "base64Img"; ValueType: string; ValueName: ""; ValueData: "THM"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "thm\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; fpx
Root: HKCR; Subkey: ".fpx"; ValueType: string; ValueName: "FlashPix Bitmap Image File"; ValueData: "thm"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "base64Img"; ValueType: string; ValueName: ""; ValueData: "FPX"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "fpx\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; pcd
Root: HKCR; Subkey: ".pcd"; ValueType: string; ValueName: ""; ValueData: "pcd"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "base64Img"; ValueType: string; ValueName: ""; ValueData: "FPX"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "pcd\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; flif
Root: HKCR; Subkey: ".flif"; ValueType: string; ValueName: "Free Lossless Image Format File"; ValueData: "pcd"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "base64Img"; ValueType: string; ValueName: ""; ValueData: "FLIF"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "flif\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

;COMIC BOOKS!!

; cbr
Root: HKCR; Subkey: ".cbr"; ValueType: string; ValueName: ""; ValueData: "CbrImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "CbrImg"; ValueType: string; ValueName: ""; ValueData: "Comic Book RAR Archive"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "CbrImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; cbr
Root: HKCR; Subkey: ".cbr"; ValueType: string; ValueName: ""; ValueData: "CbrImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "CbrImg"; ValueType: string; ValueName: ""; ValueData: "Comic Book RAR Archive"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "CbrImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; cb7
Root: HKCR; Subkey: ".cb7"; ValueType: string; ValueName: ""; ValueData: "Cb7Img"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "Cb7Img"; ValueType: string; ValueName: ""; ValueData: "Comic Book 7-Zip Archive"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "Cb7Img\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; cbt
Root: HKCR; Subkey: ".cbt"; ValueType: string; ValueName: ""; ValueData: "CbtImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "CbtImg"; ValueType: string; ValueName: ""; ValueData: "Comic Book TAR File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "CbtImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; cbz
Root: HKCR; Subkey: ".cbz"; ValueType: string; ValueName: ""; ValueData: "CbzImg"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "CbzImg"; ValueType: string; ValueName: ""; ValueData: "Comic Book Zip Archive"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}\DefaultIcon";             ValueData: "{app}\{#MyAppExeName},0";               ValueType: string;  ValueName: ""
Root: HKCR; Subkey: "CbzImg\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
