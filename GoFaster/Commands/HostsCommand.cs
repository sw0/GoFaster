using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class HostsCommand : BaseCommand
    {
        public HostsCommand(GoFasterContext context) : base(context)
        {
        }

        public const string HostsFile = @"C:\Windows\System32\drivers\etc\hosts";


        public override void Register(CommandLineApplication app)
        {
            new[] { "hosts", "host" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    var setCmd = c.Command("set", setConfig =>
                    {
                        var envs = setConfig.Argument("<ENVIRONMENT>", "environments", true);
                        setConfig.Description = "set hosts file";

                        setConfig.OnExecute(() =>
                        {
                            var tp = envs.Values;
                            //TODO PERMISSION

                            return 0;
                        });
                    });

                    var folderCmd = c.Command("folder", setConfig =>
                    {
                        //var envs = setConfig.Argument("<ENVIRONMENT>", "environments", true);
                        setConfig.Description = "open hosts directory";

                        setConfig.OnExecute(() =>
                        {
                            var folder = Path.GetDirectoryName(HostsFile);
                            //TODO PERMISSION
                            Process.Start(folder);

                            return 0;
                        });
                    });

                    var rootArg = c.Argument("<ROOT>", "root", false);
                    var envsArg = c.Argument("<ENVIRONMENT>", "environments", false);
                    c.Description = "open hosts file";

                    c.OnExecute(() =>
                    {
                        var root = rootArg.Value;
                        var envsVals = envsArg.Value;

                        OpenHosts(HostsFile);
                        Console.Out.WriteLine("sdfsf");

                        return 0;
                    });
                })
            );
        }


        private void OpenHosts(string fileName)
        {

            Process.Start("notepad.exe", HostsFile);
        }
    }
}
