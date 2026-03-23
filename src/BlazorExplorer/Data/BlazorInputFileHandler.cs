/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AnyUi;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static BlazorExplorer.Pages.PanelPackageContainerList;
using AdminShellNS;
using System;

namespace BlazorUI.Data
{
    /// <summary>
    /// This class handles the automation of a Blazor <c>InputFile</c> component.
    /// </summary>
    public class BlazorInputFileHandler
    {
        const string DefaultHint = "Drop a file here to upload it, or click to choose a file";
        const long MaxFileSize = 500 * 1024 * 1024; // 500MB

        /// <summary>Short status line shown above the hint (e.g. filename or error).</summary>
        public string StatusLine = "";

        /// <summary>Hint shown below the status line.</summary>
        public string Hint = DefaultHint;

        /// <summary>Legacy property kept for compatibility.</summary>
        public string Status => StatusLine.Length > 0 ? StatusLine + " – " + Hint : Hint;

        /// <summary>
        /// Set to <see cref="BlazorSession.SessionTempDirectory"/> so uploads do not collide across circuits.
        /// If unset, falls back to <see cref="System.IO.Path.GetTempPath"/>.
        /// </summary>
        public string UploadBaseDirectory { get; set; }

        public Func<AnyUiDialogueDataOpenFile, Task> FileDropped { get; set; }

        /// <summary>Reset drop zone text after the main package was closed (avoid stale "… uploaded.").</summary>
        public void ClearUploadBanner()
        {
            StatusLine = "";
            Hint = DefaultHint;
        }

        public async Task FileSelected(IBrowserFile file)
        {
            try
            {
                if (file == null)
                {
                    return;
                }
                else if (file.Size > MaxFileSize)
                {
                    StatusLine = $"Too big (max {MaxFileSize / (1024 * 1024)} MB)";
                    Hint = DefaultHint;
                }
                else
                {
                    StatusLine = "Loading…";
                    Hint = "";

                    var baseDir = UploadBaseDirectory;
                    if (string.IsNullOrEmpty(baseDir))
                        baseDir = System.IO.Path.GetTempPath();
                    var fn = System.IO.Path.Combine(
                                baseDir,
                                System.IO.Path.GetFileName(file.Name));
                    using (var readStream = file.OpenReadStream(MaxFileSize))
                    using (var fileStream = System.IO.File.Create(fn))
                        await readStream.CopyToAsync(fileStream);
                    // fileStream fully closed before AASX loader tries to copy/open it

                    var ddof = new AnyUiDialogueDataOpenFile();
                    ddof.OriginalFileName = file.Name;
                    ddof.TargetFileName = fn;

                    StatusLine = System.IO.Path.GetFileName(file.Name) + " uploaded.";
                    Hint = DefaultHint;

                    await FileDropped?.Invoke(ddof);
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
        }
    }
}
