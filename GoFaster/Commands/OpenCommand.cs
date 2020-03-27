using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class OpenCommand : BaseCommand
    {
        public string Name { get; set; } = "Open";

        public OpenCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "open", "o" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "open given project";

                    //var projectOpt = c.Option("--name -n <PROJECT_NAME>", "[Optional] project name", CommandOptionType.SingleValue);
                    var branchOpt = c.Option("--branch -b <BRANCH_NAME>", "[Optional] branch name", CommandOptionType.SingleValue);

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

                        var query = Context.Profile.Projects.AsQueryable();

                        query = query.Where(p => FilterPredict(p.Name, filter));

                        var rows = query.ToList();

                        if(rows.Count > 0)
                        {
                            Console.WriteLine($"Got {rows.Count} projects: {string.Join(',', rows.Select(p => p.Name))}");

                            var project = rows.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Path));
                            
                            if(project == null)
                            {
                                Console.WriteLine($"No valid code path configured for projects found");
                                return 0;
                            }

                            var p = new Process
                            {
                                StartInfo = new ProcessStartInfo(project.Path)
                                {
                                    UseShellExecute = true
                                }
                            };
                            p.Start();
                        }

                        return 0;
                    });
                })
            );
        }
    }
}
