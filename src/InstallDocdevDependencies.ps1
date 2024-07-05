<#*******************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************#>

<#
.SYNOPSIS
This script installs the dependencies needed to generate documentation for developers.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    GetToolsDir


function Main
{
    if ($null -eq (Get-Command "nuget.exe" -ErrorAction SilentlyContinue))
    {
       throw "Unable to find nuget.exe in your PATH"
    }
    
    AssertDotnet

    Set-Location $PSScriptRoot
    $toolsDir = GetToolsDir
    New-Item -ItemType Directory -Force -Path $toolsDir|Out-Null

    Write-Host "Installing DocFX 2.56.1 ..."
    nuget install docfx.console -Version 2.56.1 -OutputDirectory $toolsDir

    dotnet tool restore
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
