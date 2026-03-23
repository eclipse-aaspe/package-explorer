/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).
*/

using System;
using System.IO;
using AasxPackageLogic;

namespace BlazorUI.Data
{
    /// <summary>
    /// Central place for Blazor app temp storage: one folder under the system temp directory,
    /// cleared on process start, with one subdirectory per <see cref="BlazorSession"/>.
    /// </summary>
    public static class BlazorAppTempPaths
    {
        /// <summary>Folder name under <see cref="Path.GetTempPath()"/>.</summary>
        public const string AppFolderName = "AasxBlazorExplorer";

        /// <summary>Root directory for this app instance (after <see cref="InitializeAtStartup"/>).</summary>
        public static string RootPath { get; private set; }

        static BlazorAppTempPaths()
        {
            RootPath = Path.Combine(Path.GetTempPath(), AppFolderName);
        }

        /// <summary>
        /// Deletes the entire app temp tree and recreates an empty root folder.
        /// Call once when the web host starts so leftover files from a previous run are removed.
        /// </summary>
        public static void InitializeAtStartup()
        {
            RootPath = Path.Combine(Path.GetTempPath(), AppFolderName);
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex,
                    $"Could not fully delete app temp directory {RootPath}; continuing with best-effort cleanup.");
                TryDeleteContentsBestEffort(RootPath);
            }

            try
            {
                Directory.CreateDirectory(RootPath);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"Could not create app temp directory {RootPath}");
                throw;
            }

            Log.Singleton.Info($"Blazor app temp root (cleared on startup): {RootPath}");
        }

        static void TryDeleteContentsBestEffort(string dir)
        {
            if (!Directory.Exists(dir))
                return;
            try
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    try { File.Delete(f); } catch { /* ignore */ }
                }
                foreach (var d in Directory.GetDirectories(dir))
                {
                    try { Directory.Delete(d, recursive: true); } catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Ensures <c>{RootPath}/session-{sessionId}/</c> exists and returns its full path.
        /// </summary>
        public static string EnsureSessionDirectory(int sessionId)
        {
            if (sessionId < 1)
                throw new ArgumentOutOfRangeException(nameof(sessionId));

            var path = Path.Combine(RootPath, $"session-{sessionId}");
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>Best-effort delete of a session subdirectory when the circuit ends.</summary>
        public static void TryDeleteSessionDirectory(string sessionDirectory)
        {
            if (string.IsNullOrWhiteSpace(sessionDirectory))
                return;
            try
            {
                if (Directory.Exists(sessionDirectory)
                    && sessionDirectory.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Delete(sessionDirectory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Info($"Session temp directory not removed (may still be in use): {sessionDirectory} — {ex.Message}");
            }
        }
    }
}
