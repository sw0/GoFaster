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
        public ListCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            app.Command("list", c =>
            {
                c.Description = "list projects";

                var optionName = c.Option("--name -n <PROJECT_NAME>", "[Optional] project name", CommandOptionType.SingleValue);
                var optionOwners = c.Option("--team -t <TEAM_NAME>", "[Optional] team name", CommandOptionType.MultipleValue);
                var optionCategory = c.Option("--category -c <CATETORY>", "[Optional] category", CommandOptionType.SingleValue);

                c.HelpOption("-?|-h|--help");

                c.OnExecute(() =>
                {
                    var projects = Context.Profile.Projects;

                    IQueryable<Project> query = QueryProjects(optionName.Value(), optionOwners.Values,
                        optionCategory.Values);

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
