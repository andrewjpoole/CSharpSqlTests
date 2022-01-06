﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
#pragma warning disable CS1591

namespace CSharpSqlTests
{
    public class DacPacInfo
    {
        public string DacPacPath { get; } = "";
        public string DacPacProjectName { get; }
        public bool DacPacFound { get; }

        public DacPacInfo(string dacPacName, int maxSearchDepth, Action<string>? logTiming = null)
        {
            var maxSearchDepth1 = maxSearchDepth;
            var logTiming1 = logTiming;
            DacPacProjectName = dacPacName;

            // first check if an absolute path to a file has been supplied
            var dacPacAbsolutePath = new FileInfo(dacPacName);
            if (dacPacAbsolutePath.Exists)
            {
                DacPacPath = dacPacName;
                DacPacProjectName = dacPacAbsolutePath.Name;
                DacPacFound = true;
                return;
            }

            if (dacPacName.Contains("\\"))
                throw new FileNotFoundException(
                    $"The DacPac name contains slash(es) which suggests its an absolute path, however no file exists at {dacPacName}");

            // else search for a dacpac of the specified name
            var currentDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);

            logTiming1?.Invoke($"Starting search for DacPacs at {currentDirectory}.");
            logTiming1?.Invoke($"Attempting to traverse up {maxSearchDepth1} directories.");

            // first traverse up to hopefully the solution directory
            var directoryInfo = currentDirectory;
            for (var x = 0; x < maxSearchDepth1; x++)
            {
                if(directoryInfo.Parent is not null)
                    directoryInfo = directoryInfo.Parent;
            }

            // search for dacpac files
            logTiming1?.Invoke($"Searching for DacPacs in all files under {directoryInfo.FullName}, if this is too high please change the value of maxSearchDepth.");
            var dacPacs = directoryInfo.EnumerateFiles($"{dacPacName}.dacpac", SearchOption.AllDirectories).ToList();

            if (dacPacs.Any())
            {
                logTiming1?.Invoke($"Found {dacPacs.Count} DacPac file.");

                DacPacPath = dacPacs.First().FullName;
                DacPacFound = true;
                logTiming1?.Invoke($"Using DacPac file {DacPacPath}.");
            }

            logTiming1?.Invoke($"No DacPacs found matching {dacPacName}.");
        }
    }
}