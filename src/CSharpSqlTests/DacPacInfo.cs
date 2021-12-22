using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSharpSqlTests
{
    public class DacPacInfo
    {
        public string DacPacPath { get; } = "";
        public string DacPacProjectName { get; }
        public bool DacPacFound { get; }

        public DacPacInfo(string dacPacName)
        {
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

            // first traverse up to hopefully the solution directory
            var directoryInfo = currentDirectory;
            for (var x = 0; x < 6; x++)
            {
                if(directoryInfo.Parent is not null)
                    directoryInfo = directoryInfo.Parent;
            }

            // search for dacpac files
            var dacPacs = directoryInfo.EnumerateFiles($"{dacPacName}.dacpac", SearchOption.AllDirectories).ToList();

            if (dacPacs.Any())
            {
                DacPacPath = dacPacs.First().FullName;
                DacPacFound = true;
            }
        }
    }
}