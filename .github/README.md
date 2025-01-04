<h1 align="center">
<img src="https://d33wubrfki0l68.cloudfront.net/327934f4ff80060e07c17935683ecad27cda8080/ee2bc/assets/images/photoshop_1.png" alt="PicView Logo" height="90">
</h1>

PicView is a fast, free and fully customizable image viewer for Windows 10 and 11. It supports a vast range of image file types, including `WEBP`, `GIF`, `SVG`, `PNG`, `JXL`, `HEIC`, `PSD` and many others. 

Additional features includes viewing EXIF metadata, image compression, batch resizing, viewing images within archives and comic books, image effects, image galleries, and more.

<p align=center>
    <a href="https://github.com/Ruben2776/PicView/releases">
        <img alt="Downloads shield" src="https://img.shields.io/github/downloads/Ruben2776/PicView/total?color=%23007ACC&label=downloads&style=flat-square">
    </a>
    <a href="https://github.com/Ruben2776/PicView/blob/master/LICENSE.txt">
        <img alt="GPL v3 License" src="https://img.shields.io/badge/license-GPLv3-green.svg?maxAge=3600&style=flat-square">
    </a>
    <img alt="Windows OS" src="https://img.shields.io/badge/OS-Windows%2010/11%2064%20bit-00adef.svg?maxAge=3600&style=flat-square">
</p>

# Project status
PicView is currently being rewritten to the Avalonia platform. You can read more [here](https://github.com/Ruben2776/PicView/issues/159) or download the preview versions [https://picview.org/avalonia-download](https://picview.org/avalonia-download)

# Downloads

[![](https://img.shields.io/badge/Windows-x64-blue?style=flat-square&logo=windows&logoColor=fff)](https://github.com/Ruben2776/PicView/releases/download/2.3.3/PicView-v2.3.3-win-x64.zip) [![](https://img.shields.io/badge/Windows-arm64-blue?style=flat-square&logo=windows&logoColor=fff)](https://github.com/Ruben2776/PicView/releases/download/2.3.3/PicView-v2.3.3-win-arm64.zip)

Latest releases at https://picview.org/download

**Mirrors**

[FossHub](https://www.fosshub.com/PicView.html) <br>
[MajorGeeks](https://www.majorgeeks.com/files/details/picview.html)  <br>
[SourceForge](https://sourceforge.net/projects/picview/) <br>
[uptodown](https://picview.en.uptodown.com/windows)


Winget:
```cmd
cmd $> winget install picview
```

Chocolatey:
```cmd
cmd $> choco install picview
```

Scoop:
```cmd
cmd $> scoop bucket add extras
cmd $> scoop install extras/picview
```


## Code Signing Policy
Starting with v3.0.0-rc-preview-4 the builds are digitally signed.

Free code signing is provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

# Features and screenshots
![configure-ui02 - Copy (2)](https://github.com/Ruben2776/PicView/assets/4200419/2459b663-ca42-415c-8d98-b6f45da0134c)

Switch between a dark and a light theme and toggle between hiding the UI

![UI-Dark-Theme-Magenta](https://github.com/Ruben2776/PicView/assets/4200419/c2bf8c6f-35bc-487c-baef-26df3b35fb82)

_UI overview with bottom gallery_


<h3 align="center">
    Scroll Image
</h3>

<h1 align="center">
    <img src="https://picview.org/assets/screenshots/scroll/large/PicView%20-%20Scroll%20Dark%20Theme%2001.webp" />
</h1>

Press `X` to toggle the scroll function. Click the mousewheel for auto scroll.

<h3 align="center">
    Crop Image
</h3>

<h1 align="center">
    <img src="https://picview.org/assets/screenshots/PicView-Crop/PicView%20Crop%20Dark%20Theme.webp" />
</h1>


Quickly crop image by pressing `C`. Hold `Shift` for square selection.


<h3 align="center">
    Image Info Window
</h3>

<h1 align="center">
    <img src="https://picview.org/assets/screenshots/EXIF-image-Info-Dark.webp" />
</h1>

Lossleslly compress current image by pressing the __Optimize Image__ button.

Click on the stars to save EXIF image rating.

Rename or move files by editing the text box values.

__Resize image:__

Edit the Width and Height boxes to rezise image.

Use % to resize it by percentage.

#### EXIF:

Click the expander button to view GPS coordinates which links to Google or BING maps, including advanced camera info, image info, authors, copyright etc.



<h3 align="center">
    Image Gallery
</h3>

<h1 align="center">
    <img src="https://picview.org/assets/screenshots/Horizontal%20Gallery/Horizontal%20Gallery%202023-04-26%20175830.png" />
</h1>


### Press `G` to open or close the image gallery

Navigate the gallery with the `arrow keys` or `W`,`A`,`S`,`D` and load the selected image with `Enter` or the `E` key.
The bottom gallery can be turned on or off


<h3 align="center">
    Image filters
</h3>

<h1 align="center">
    <img src="https://d33wubrfki0l68.cloudfront.net/7fa49db824f06b6b0f7ff10c299560149b36416f/3dc08/assets/video/hlsl-v2-800w.webp" />
</h1>


### Use the slider to change the intensity of the effect


Save it locally, set is as wallpaper/lock-screen image, or copy it to clip-holder with the effect applied.


<h3 align="center">
    Batch Resizing
</h3>

<h1 align="center">
    <img src="https://picview.org/assets/screenshots/batch%20resize/batch%20resize%20dark%202023-05-12%20121358.webp" />
</h1>


### Convert/Optimize all your pictures
All files from the `Source folder` will be selected for processing and will be sent to `Output folder`. The default name for the output folder will be **Processed Pictures**.

If the *Output folder* is the same as the *Source folder*, or left blank, the files will be overwrittten.

The `Convert to` dropdown option allows you to convert all the files to a popular format.

The `Compression` dropdown option allows you to compress the files, either without losing quality or sacrifing some quality for greater reduced file size.

The `Quality` dropdown option allows you to change quality of supported file types. The higher the Quality setting, the more detail is preserved in the image, but the larger the file size.

The `Resize` dropdown option allows you to resize the picture by **height**, **width** and **percentage** while keeping the aspect ratio of the image.

___

### Other features

If you have 7-Zip or WinRAR installed, you can view images inside archives, such as `.zip`, `.rar`, etc, and comic book archives (`.cbr`, `.cb7`, `.cbt`, .`cbz`, `.cba`).

* Quick startup time and built-in preloader to instantly view next image
* For images with a transparent background, the background can be changed to a checkerboard background, a dark background or a white background by pressing `B`.
* Interface can be toggled to just show the image by pressing `Alt + Z`.
* Image EXIF rating
* Preview between 27 different image filters that will be applied when copying image or setting it as wallpaper/lockscreen image or saving file locally
* Image Galleries
* Sort files by: `name`, `file size`, `date created`, `last accessed`, `latest edit`, `file extension` and `randomized`
* hover buttons can be toggled on/off in the settings window.
* Basic editing: rotate, flip, crop, resize, change file type
* Stay on top of other windows
* Search subdirectories
* Drag & drop/paste from clipholder support for files, folders, URLs and archives
* Scroll function (built with manga/comics in mind)
* Open file in external application, show it in folder or view file properties
* Color picker

<img src="https://picview.org/assets/screenshots/rename-titlebar/rename-titlebar-pink-dark.webp"/><br>
Rename or move files in the titlebar by pressing `F2` or right clicking it.

**File support** 
 > .jpg  .jpeg  .jpe  .png  .bmp  .tif  .tiff  .gif  .ico  .jfif  .webp .svg .svgz <br>
   .psd  .psb .xcf .jxl .heic .heif .jp2 .hdr .tga .dds<br>.3fr  .arw  .cr2 .cr3  .crw  .dcr  .dng  .erf  .kdc  .mdc  .mef  .mos  .mrw  .nef  .nrw  .orf  .pef .raf  .raw  .rw2  .srf  .x3f *<br>
   .pgm  .hdr  .cut  .exr  .dib  .emf  .wmf  .wpg  .pcx  .xbm  .xpm .wbmp *
   
   _* RAW camera formats may be slower to load_

## Default Shortcuts
_* Shortcuts can be changed by opening the `About` window, and scrolling down_
| Shortcut             | Explanation                                                                                                      |
| -------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Esc                  | Close window or current open menu                                                                                |
| Ctrl + Q             | Exit the application                                                                                             |
| F1                   | Open the about window                                                                                            |
| F2                   | Rename or move the current file                                                                                  |
| F3                   | Open and select current file in Explorer                                                                         |
| F4                   | Open the settings window                                                                                         |
| F5                   | Start slideshow                                                                                                  |
| F6                   | Open the effects window                                                                                          |
| F7                   | Reset Zoom                                                                                                       |
| F9                   | Open the batch resize window                                                                                     |
| F11                  | Toggle fullscreen                                                                                                 |
| F12                  | Toggle viewing fullscreen gallery                                                                                 |
| Alt + Enter          | Toggle fullscreen                                                                                                 |
| C                    | Crop Image                                                                                                       |
| Ctrl + C             | Copy image or copy cropped image                                                                                 |
| Ctrl + V             | Paste from clipholder, URL, File, File Path or Image                                                             |
| Ctrl + X             | Cut image file into clipboard                                                                                    |
| O, Ctrl + O          | Open file picker dialog                                                                                          |
| Ctrl + S             | Save as file                                                                                                     |
| B                    | Toggle background color                                                                                          |
| X                    | Toggle scrolling the image                                                                                       |
| F                    | Flip the image                                                                                                   |
| J                    | Open the image resize function                                                                                   |
| Del                  | Send current file to the recycle bin                                                                             |
| Shift + Del          | Permanently delete current file                                                                                  |
| I                    | Show the image info window                                                                                       |
| Ctrl + I             | Show file properties                                                                                             |
| Ctrl + Alt + I       | Open the image resize function                                                                                   |
| Ctrl + P             | Print the image                                                                                                  |
| R                    | Reset zoom                                                                                                       |
| Ctrl + R             | Reload                                                                                                           |
| L                    | Toggle looping                                                                                                   |
| E                    | Open with another application (opens highlighted image if in gallery view)                                       |
| T                    | Toggle if the applications should stay above other windows                                                       |
| N                    | Open the batch resize window                                                                                     |
| Ctrl + N             | Open new window                                                                                                  |
| G                    | Toggles the gallery view                                                                                         |
| Space                | Centers window on the current screen (if gallery is open, it will scroll to the center of the highlighted image) |
| 1                    | Turns on the `Auto fit window` and sets `Fill image` off                                                         |
| 2                    | Turns on the `Auto fit window` and sets `Fill image` on                                                          |
| 3                    | Turns off the `Auto fit window` and sets `Fill image` off                                                        |
| 4                    | Turns off the `Auto fit window` and sets `Fill image` on                                                         |
| Home                 | Scrolls to the top, when scrolling is enabled                                                                    |
| End                  | Scrolls to the bottom, when scrolling is enabled                                                                 |
| Enter                | Opens highlighted image if in gallery view                                                                       |
| A                    | Navigate to previous image                                                                                       |
| Left                 | Navigate to previous image                                                                                       |
| Ctrl + A             | Navigate to first image                                                                                          |
| Ctrl + Left          | Navigate to first image                                                                                          |
| D                    | Navigate to next image                                                                                           |
| Right                | Navigate to next image                                                                                           |
| Ctrl + D             | Navigate to last image                                                                                           |
| Ctrl + Right         | Navigate to first image                                                                                          |
| Ctrl + Shift + Right | Navigate to next folder                                                                                          |
| Ctrl + Shift + Left  | Navigate to previous folder                                                                                      |
| Ctrl + Shift + D     | Navigate to next folder                                                                                          |
| Ctrl + Shift + A     | Navigate to previous folder                                                                                      |
| Up ⇔ W              | Rotates the image up (clockwise)                                                                                 |
| Up ⇔ W              | Scrolls up when scrolling is enabled                                                                             |
| PageUp               | Scrolls up when scrolling is enabled                                                                             |
| Down ⇔ S            | Rotates image down (counterclockwise)                                                                            |
| Down ⇔ S            | Scrolls down when scrolling is enabled                                                                           |
| PageDown             | Scrolls down when scrolling is enabled                                                                           |
| +                    | Zooms in at cursor position                                                                                      |
| -                    | Zooms out at cursor position                                                                                     |
| Double click         | Reset zoom                                                                                                       |
| Mouse Browserback    | Go to the previous entry in the file history                                                                     |
| Mouse Browserforward | Go to the next entry in the file history                                                                         |
| Scrollwheel\*        | Navigates back or forth                                                                                          |
| Ctrl + Scrollwheel   | Zoom in or out                                                                                                   |
| Alt + Z              | Show or hide UI                                                                                                  |

_* Scrollwheel behavior can be changed in settings_

You can also view and change shortcuts by opening the `About` window `F1`


# Contributions
![Visual Studio 2022](https://img.shields.io/badge/IDE-Visual%20Studio%202022-964ad4.svg?maxAge=3600)
![.Net](https://img.shields.io/badge/.NET-5C2D91?style=badge&logo=.net&logoColor=white)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/bf0fd0f740f9486ba306bdec7fe8bde7)](https://www.codacy.com/manual/ruben_8/PicView?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=Ruben2776/PicView&amp;utm_campaign=Badge_Grade)

**Building:** <br>
Open and run the solution in Visual Studio or Rider. 
If you're not using x64 hardware, make sure to change the platform target to your CPU architecture in the project properties, as well as changing the the Magick.NET Nuget packages to match.

Pull requests are welcome. Check current issues and assign yourself or create your own issue. 

Improvements to the current code or bug fixes are also welcome!


## Translators/Languages
Chinese by <a href="https://github.com/Crystal-RainSlide">Crystal-RainSlide</a>, <a href="https://github.com/wcxu21">wcxu21</a><br>
Spanish by <a href="https://github.com/lk-KEVIN">lk.KEVIN</a> <i>(needs updates)</i><br>
Korean by <a href="https://github.com/VenusGirl">VenusGirl</a><br>
German by <a href="https://github.com/Brotbox">Brotbox</a>, [uDEV2019](https://github.com/uDEV2019)<br>
Polish by <a href="https://github.com/YourSenseiCreeper">YourSenseiCreeper</a><br>
French by <a href="https://www.challenger-systems.com/2021/11/picview-156.html">Sylvain LOUIS</a> <br>
Italian by <a href="https://github.com/franpoli">franpoli</a> <br>
Russian by <a href="https://github.com/andude10">andude10</a> <br>
Romanian by <a href="https://crowdin.com/profile/lmg">M. Gabriel Lup</a> <br>
Swedish by <a href="https://github.com/sparmark">Stefan Parmark</a> <br>
Brazilian Portuguese by <a href="https://github.com/andercard0">Anderson Cardoso</a> <br>
English and Danish by me<br>

**Looking for translators!**
If you want to help translate another language or update/improve a current one and be listed here, please take a look at
https://github.com/Ruben2776/PicView/issues/13


## Privacy Policy

PicView does not collect data. No data is sent/recieved and/or collected by PicView.


# Donate
If you wish to thank me for my work, please

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/W7W46BJFV)
