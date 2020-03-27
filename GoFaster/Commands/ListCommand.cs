using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class ListCommand : BaseCommand
    {
        public string Name { get; set; } = "list";

        public ListCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            app.Command("list", c =>
            {
                c.Description = "list projects";

                var optionName = c.Option("--name -n <PROJECT_NAME>", "[Optional] project name", CommandOptionType.SingleValue);
                var optionTeams = c.Option("--team -t <TEAM_NAME>", "[Optional] team name", CommandOptionType.MultipleValue);
                var optionCategory = c.Option("--category -c <CATETORY>", "[Optional] category", CommandOptionType.SingleValue);

                c.HelpOption("-?|-h|--help");

                c.OnExecute(() =>
                {
                    var projects = Context.Profile.Projects;

                    IQueryable<Project> query = projects.AsQueryable();

                    if (optionName.HasValue())
                    {
                        var filter = optionName.Value();
                        query = query.Where(r => r.Name != null
                        && FilterPredict(r.Name, filter));
                    }

                    if (optionName.HasValue())
                    {
                        var filter = optionName.Value();

                        query = query.Where(r =>
                            r.Name != null && FilterPredict(r.Name, filter)
                        );
                    }

                    if (optionTeams.HasValue())
                    {
                        query = query.Where(r =>
                            r.Name != null &&
                            optionTeams.Values.Any(filter => FilterPredict<Project>(r, o => o.Owners, filter))
                        );
                    }

                    if (optionCategory.HasValue())
                    {
                        query = query.Where(r =>
                            r.Name != null &&
                            FilterPredict(r, o => o.Categories, optionCategory.Value())
                        );
                    }

                    foreach (var p in query)
                    {
                        Console.WriteLine(p.GetTitle());
                    }
                    return 0;
                });
            });
        }
    }
}
