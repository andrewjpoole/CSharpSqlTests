using System.IO;
using System.Linq;
using System.Reflection;

namespace CSharpSqlTests
{
    public class DacPacInfo
    {
        public string DacPacPath { get; }
        public string DacPacProjectName { get; }
        public bool DacPacFound { get; }

        public DacPacInfo(string dacPacName)
        {
            DacPacProjectName = dacPacName;

            // first check if an absolute path to a file has been supplied
            var dacpacAbsolutePath = new FileInfo(dacPacName);
            if (dacpacAbsolutePath.Exists)
            {
                DacPacPath = dacPacName;
                DacPacProjectName = dacpacAbsolutePath.Name;
                DacPacFound = true;
                return;
            }

            // else search for a dacpac of the specified name
            var currentDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
            var solutionDir = currentDirectory.Parent?.Parent?.Parent?.Parent?.Parent;

            var dacPacs = solutionDir.EnumerateFiles($"{dacPacName}.dacpac", SearchOption.AllDirectories).ToList();

            if (dacPacs.Any())
            {
                DacPacPath = dacPacs.First().FullName;
                DacPacFound = true;
            }
        }
    }
}