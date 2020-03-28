using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GoFaster.Utils
{
    public class VSHelper
    {
        public static List<string> FindVSLocations()
        {
            List<string> exeFiles = null;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var vsBaseDir = @"C:\Program Files (x86)\Microsoft Visual Studio\";
                var subDirs = Directory.GetDirectories(vsBaseDir, "20*");
                var versions = new[] { "Enterprise", "Professional", "Community" };
                //todo cache these exe locations
                exeFiles = subDirs.ToList().SelectMany(dir =>
                {
                    var exeList = new List<string>();
                    foreach (var ver in versions)
                    {
                        var exefile = dir + $@"\{ver}\Common7\IDE\devenv.exe";
                        if (File.Exists(exefile))
                            exeList.Add(exefile);
                    }
                    return exeList;
                }).ToList();

                var legacyVS = new[] { "9.0", "10.0", "11.0", "12.0", "14.0" };
                foreach (var v in legacyVS)
                {
                    var file = $"C:\\Program Files (x86)\\Microsoft Visual Studio {v}\\Common7\\IDE\\devenv.exe";
                    if (File.Exists(file))
                    {
                        exeFiles.Add(file);
                    }
                }
            }

            return exeFiles ?? new List<string>();
        }
    }
}
