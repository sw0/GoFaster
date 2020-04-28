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
                        var optionEnvironment = setConfig.Option("--environment -e <ENVIRONMENT>", "[Optional] enviroment", CommandOptionType.SingleValue);

                        c.HelpOption("-?|-h|--help");

                        var envs = setConfig.Argument("<STACKS>", "stacks", true);
                        setConfig.Description = "set hosts file base on given stack(s) and environment information";

                        setConfig.OnExecute(() =>
                        {
                            var tp = envs.Values;

                            WriteLine("stacks: " + string.Join(",", envs.Values ?? new List<string> {"" }));
                            WriteLine(optionEnvironment.Value());

                            return 0;
                        });
                    });

                    var folderCmd = c.Command("folder", setConfig =>
                    {
                        //var envs = setConfig.Argument("<ENVIRONMENT>", "environments", true);
                        setConfig.Description = "open hosts directory";

                        setConfig.OnExecute(() =>
                        {
                            if (!IsWinows)
                            {
                                WriteLine("Aborted. Only supported on Windows");
                                return 0;
                            }

                            var folder = Path.GetDirectoryName(HostsFile);

                            Process.Start("explorer.exe", folder);

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
