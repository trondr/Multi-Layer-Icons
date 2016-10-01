# Multi Layer Icons

This project builds multi layer icons (.ico) from open source vector icons (.svg) using the command:

```cmd

NMultiTool.exe ConvertAllSvgToIco /folder="c:\Temp\svgrootsourcefolder" /recursive="True" /refresh="False" /sizes="[16;22;24;32;48;64;128;256]" /maxDegreeOfParallelism="4"
````

NMultiTool.exe uses:
* [Inkscape.exe](https://inkscape.org/en/download/?lang=en) to export .svg to .png
* [ImageMagick .NET Library](https://www.nuget.org/packages/Magick.NET-Q16-AnyCPU) to resize .png and create multilayer icon

## Multi layer icon dimensions

* 16x16
* 22x22
* 24*24
* 32x32
* 48x48
* 64x64
* 128x128
* 256x256

## Icon sources
* The icon packs used by [MahApps.Metro](https://github.com/MahApps/MahApps.Metro):

  + [Material](https://github.com/Templarian/MaterialDesign/tree/master/icons) icons (1.7.22)
  + [Modern](http://modernuiicons.com/) icons (2016-09-24)
  + [Fontawesome](http://fontawesome.io/icons/) icons (v4.6)
  + [Entypo+](http://www.entypo.com/) icons

* Custom icons derived from existing icon packs or created from scratch by trondr

  + [trondr](https://github.com/trondr/Icons/src/Custom)
  
## Minimum Build Requirements

* [MSBuild Tools 2015](http://www.microsoft.com/en-us/download/details.aspx?id=48159)
* [IncScape.exe](https://inkscape.org/en/download/?lang=en) installed to "C:\Program Files\inkscape\inkscape.exe" (or update NMultiTool.exe.config to reflect inkscape.exe location on your system).

## Build Multi-Layer-Icons

Run:

```cmd

Build.cmd
```

Multi layer icons and corresponding png files for each dimension will be creatd in the '.\bin\Release\Multi-Layer-Icons' folder.

## Build And Deploy Multi-Layer-Icons

Run:

```cmd

BuildAndDeploy.cmd
```

Multi layer icons and corresponding png files for each dimension will be creatd in the '.\bin\Release\Multi-Layer-Icons' folder. Both release files and source will be compressed into respective zip archives.

```cmd

.\bin\Release\Multi-Layer-Icons.<version>.zip
.\bin\Release\Multi-Layer-Icons.Source.<version>.zip
```
