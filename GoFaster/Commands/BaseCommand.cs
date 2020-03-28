using GoFaster.Models;
using GoFaster.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public abstract class BaseCommand
    {
        protected GoFasterContext Context { get; }

        protected BaseCommand(GoFasterContext context)
        {
            Context = context;
        }

        public abstract void Register(CommandLineApplication app);

        #region FilterPredict

        protected bool FilterPredict(string value, string filter)
        {
            if (string.IsNullOrEmpty(filter)) throw new ArgumentException(nameof(filter));
            if (string.IsNullOrEmpty(value)) return false;

            Regex regex = new Regex(filter, RegexOptions.IgnoreCase);

            if (regex.IsMatch(value)) return true;

            return false;
        }

        protected bool FilterPredict<T>(T o, Func<T, string> p, string filter)
        {
            var value = p(o);

            if (string.IsNullOrEmpty(filter)) throw new ArgumentException(nameof(filter));
            if (string.IsNullOrEmpty(value)) return false;

            Regex regex = new Regex(filter, RegexOptions.IgnoreCase);

            if (regex.IsMatch(value)) return true;

            return false;
        }

        protected bool FilterPredict<T>(T o, Func<T, IEnumerable<string>> func, string filter)
        {
            var values = func(o);

            if (string.IsNullOrEmpty(filter)) throw new ArgumentException(nameof(filter));

            if (values == null || values.Count() == 0) return false;

            Regex regex = new Regex(filter, RegexOptions.IgnoreCase);

            if (values.Any(v => regex.IsMatch(v))) return true;

            return true;
        }
        #endregion

        #region IConsole/IApplication

        protected void WriteLine(string msg)
        {
            Console.WriteLine(msg);
        }
        protected void WriteLine(string format, object arg)
        {
            Console.WriteLine(format, arg);
        }

        protected void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }


        protected ConfirmOption Confirm(string title, ConfirmOption[] options)
        {
            Console.WriteLine(title);
            Console.WriteLine(string.Join("\t", options.Select(o => o.Name)));

            while (true)
            {
                var reply = Console.ReadLine();

                var validReply = options.FirstOrDefault(o => o.Value.Equals(reply, StringComparison.OrdinalIgnoreCase)
                 || o.Value.Contains($"[{reply}]", StringComparison.OrdinalIgnoreCase)
                 || o.Name.Equals(reply, StringComparison.OrdinalIgnoreCase));

                if (validReply != null)
                {
                    return validReply;
                }
            }
        }

        #endregion

        #region Functions

        protected IQueryable<Project> QueryProjects(string nameOrIndex, List<string> owners, List<string> categories)
        {
            var query = Context.Profile.Projects.AsQueryable();

            if (int.TryParse(nameOrIndex, out var index))
            {
                query = query.Where(p => p.Index == index);
            }
            else
            {
                query = query.Where(p => FilterPredict(p.Name, nameOrIndex));
            }

            if (owners != null && owners.Any())
            {
                query = query.Where(r =>
                    r.Name != null &&
                    owners.Any(filter => FilterPredict(r, o => o.Owners, filter))
                );
            }

            if (categories != null && categories.Any())
            {
                query = query.Where(r =>
                    r.Name != null &&
                    categories.Any(filter => FilterPredict(r, o => o.Categories, filter))
                );
            }

            return query;
        }

        protected void Open(Project project, string branch = null)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                WriteLine("NOT SUPPORTED YET");
            }
            else
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo(project.Path)
                    {
                        UseShellExecute = true
                    }
                };
                p.Start();
            }
        }
        protected string GetDirOfProject(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            string wd = project.Path;
            if (string.IsNullOrEmpty(wd)) wd = project.Entry;

            if (!string.IsNullOrEmpty(wd))
            {
                wd = Path.GetDirectoryName(wd);
            }

            return wd;
        }

        protected void LaunchVSCmd(string workDir = null)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                WriteLine("Launcing VSCmd is not supported in current OS");
                return;
            }
            if (string.IsNullOrEmpty(workDir)) workDir = null;

            //%comspec% /k "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"
            var VSExes = VSHelper.FindVSLocations();

            var vsExe = VSExes.FirstOrDefault();
            var cmdBat = string.Empty;
            if (vsExe != null && File.Exists(cmdBat = vsExe.Replace("IDE\\devenv.exe", "Tools\\VsDevCmd.bat")))
            {
                var wd = Path.GetFullPath(vsExe).ToLower();
                var p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WorkingDirectory = workDir ?? wd.Substring(0, wd.IndexOf("\\common"));
                p.StartInfo.Arguments = $"/k \"{cmdBat}\"";
                p.StartInfo.RedirectStandardOutput = false;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                //todo output redirection
            }
        }
        #endregion
    }

    public class ConfirmOption
    {
        public static ConfirmOption Yes = new ConfirmOption { Name = "[Y]es", Value = "1" };
        public static ConfirmOption YesForAll = new ConfirmOption { Name = "[A]ll", Value = "2" };
        public static ConfirmOption Cancel = new ConfirmOption { Name = "[C]ancel", Value = "0" };

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
