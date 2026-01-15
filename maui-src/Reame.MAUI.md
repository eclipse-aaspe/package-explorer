# MAUI Explorer

## Installation

* Install MAUI Workload
* A heap of things need to be restored
** dotnet workload install maui
** dotnet workload install maui-android
** dotnet workload install maui-windows
** dotnet workload install maui-desktop
* dotnet restore

## ChatGPT Recopmmendations

Confirm MAUI and .NET 9 Workloads Installed

On the working machine, MAUI and the .NET 9 preview workloads were installed. The new machine likely does not have the required workloads restored.

Run the following commands:

dotnet --list-sdks
dotnet workload list


You must see:

A .NET 9 SDK (9.0.xxx)

Installed MAUI workloads, e.g.

maui
maui-android
maui-ios
maui-maccatalyst


If they are missing, install them:

dotnet workload install maui
dotnet workload install android


If you need windows support:

dotnet workload install maui-windows

2. Force a Restore

After installing workloads:

dotnet restore


or, if you want to reinitialize everything:

dotnet workload repair
dotnet restore --force-evaluate


This regenerates project.assets.json with the correct entries.