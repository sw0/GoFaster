using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

namespace GoFaster.Commands
{
    public class DescribeCommand : BaseCommand
    {
        public DescribeCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "desc", "describe" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "show description for given project";

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

                        if(int.TryParse(filter, out var index))
                        {
                            query = query.Where(p => p.Index == index);
                        }
                        else
                        {
                            query = query.Where(p => FilterPredict(p.Name, filter));
                        }

                        foreach (var p in query.Take(5))
                        {
                            WriteLine(p.GetDetailedDescription());
                        }
                        return 0;
                    });
                })
            );
        }
    }
}
