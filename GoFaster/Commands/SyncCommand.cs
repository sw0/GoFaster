using GoFaster.Models;
using GoFaster.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class SyncCommand : BaseCommand
    {
        static readonly char[] LineSeparaters = new[] { '\r', '\n' };
        public SyncCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "sync" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "sync the code from SCM)";

                    var optionForce = c.Option("--force -f <BRANCH_NAME>", "[Optional] force sync", CommandOptionType.SingleValue);

                    var optionBranch = c.Option("--branch -b <BRANCH_NAME>", "[Optional] branch name", CommandOptionType.SingleValue);

                    var arg = c.Argument("<PROJECT_NAME>", "Project Name, can be partial or a regular expression");

                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        if (!c.Options.Any(o => o.HasValue())
                            && string.IsNullOrEmpty(arg.Value))
                        {
                            c.ShowHelp();
                        }

                        var filter = arg.Value;

                        var query = QueryProjects(filter, null, null);

                        var rows = query.ToList();

                        if (rows.Count > 0)
                        {
                            Console.WriteLine($"Got {rows.Count} projects: {string.Join(',', rows.Select(p => p.Name))}");

                            var project = rows.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Path));

                            if (project == null)
                            {
                                WriteLine($"No valid code path configured for projects found");
                                return 0;
                            }

                            if (project.EventCommands != null
                            && project.EventCommands.Any(s => !string.IsNullOrWhiteSpace(s)))
                            {
                                var cmds = project.EventCommands.Where(s => !string.IsNullOrWhiteSpace(s));

                                foreach (var cmd in cmds)
                                {
                                    var lines = cmd.Split(LineSeparaters, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Select(s => s.Trim());
                                    
                                    var cmdx = string.Join(" && ", lines);
                                    var s = cmd.Cmd();

                                    WriteLine(s);
                                }
                            }
                            else
                            {
                                WriteLine("Sync command is not configured");
                            }

                        }

                        return 0;
                    });
                })
            );
        }
    }
}
