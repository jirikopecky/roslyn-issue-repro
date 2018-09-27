using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynIssueRepro
{
    class Program
    {
        static async Task Main()
        {
            MSBuildLocator.RegisterDefaults();

            DeleteReferencedLibDebugBinary();

            var properties = new Dictionary<string, string>
            {
                { "Configuration", "Release" },
                { "Platform", "AnyCPU" }
            };

            using (var workspace = MSBuildWorkspace.Create(properties))
            {
                workspace.LoadMetadataForReferencedProjects = true;

                var project = await workspace.OpenProjectAsync(GetInspectedProjectPath());

                var badRef = project.MetadataReferences.OfType<Microsoft.CodeAnalysis.UnresolvedMetadataReference>().FirstOrDefault();
                if (badRef != null)
                {
                    Console.WriteLine("Reference does not respect that we are inspecting release build - it still try to point to Debug DLL");
                    Console.WriteLine("Bad reference path: {0}", badRef.Reference);
                    Console.WriteLine();
                }

                // this will throw
                try
                {
                    var compilation = await project.GetCompilationAsync();
                    Console.WriteLine("This line will not execute");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                foreach (var item in workspace.Diagnostics)
                {
                    Console.WriteLine(item.ToString());
                }
            }

            Console.ReadKey();
        }

        private static void DeleteReferencedLibDebugBinary()
        {
            var solutionDir = GetInspectedSolutionDirectory();
            var binaryPath = Path.Combine(solutionDir, @"ReferencedLibrary\bin\Debug\netstandard2.0\ReferencedLibrary.dll");

            if (File.Exists(binaryPath))
            {
                File.Delete(binaryPath);
            }
        }

        private static string GetInspectedProjectPath()
        {
            var csprojPath = Path.Combine(GetInspectedSolutionDirectory(), "InspectedLibrary\\InspectedLibrary.csproj");

            var csproj = Path.GetFullPath(csprojPath);

            return csproj;
        }

        private static string GetInspectedSolutionDirectory()
        {
            string path = typeof(Program).Assembly.Location;
            var currentDir = Path.GetDirectoryName(path);

            var commonPath = currentDir.Substring(0, currentDir.IndexOf("ReproCode"));

            var solutionDirPath = Path.Combine(commonPath, "InspectedSolution");

            return Path.GetFullPath(solutionDirPath);
        }
    }
}
