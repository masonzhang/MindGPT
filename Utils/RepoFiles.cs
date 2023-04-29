using System;
using System.IO;
using System.Reflection;

namespace ElectronNET.WebApp.Utils;

internal static class RepoFiles
{
    /// <summary>
    /// Scan the local folders from the repo, looking for "samples/skills" folder.
    /// </summary>
    /// <returns>The full path to samples/skills</returns>
    internal static string SkillsPath()
    {
        bool SearchPath(string pathToFind, out string result, int maxAttempts = 10)
        {
            //Console.WriteLine("Assembly.GetExecutingAssembly().Location: " + Assembly.GetExecutingAssembly().CodeBase);
            //var currDir = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            var currDir = Path.GetFullPath("C:\\src\\MindGPT");
            Console.WriteLine(currDir);
            bool found;
            do
            {
                result = Path.Join(currDir, pathToFind);
                Console.WriteLine(result);
                found = Directory.Exists(result);
                currDir = Path.GetFullPath(Path.Combine(currDir, ".."));
            } while (maxAttempts-- > 0 && !found);

            Console.WriteLine(found);
            return found;
        }

        if (!SearchPath("Skills", out string path))
        {
            throw new Exception("Skills directory not found. The app needs the skills from the repo to work.");
        }

        return path;
    }
}