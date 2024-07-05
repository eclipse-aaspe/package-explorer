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
This script formats the code in-place.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDotnetFormatVersion

function Main
{
    AssertDotnet
    AssertDotnetFormatVersion

    Set-Location $PSScriptRoot
    dotnet format --exclude "**/DocTest*.cs"
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
