using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using GoFaster.Utils;
using System.IO;

namespace GoFaster.Commands
{
    public class VSCmdCommand : BaseCommand
    {
        //public string Name { get; set; } = "Describe";

        public VSCmdCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            (new[] { "vscmd" }).ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    //todo
                    c.Description = "launch VSCmd or launch VSCMD targeting given project";

                    var arg = c.Argument("<PROJECT_NAME>", "[Optional] Project Name, can be partial or a regular expression");

                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        var wd = string.Empty;

                        var filter = arg.Value;

                        if (!string.IsNullOrEmpty(filter))
                        {
                            var query = Context.Profile.Projects.AsQueryable();

                            if (int.TryParse(filter, out var index))
                            {
                                query = query.Where(p => p.Index == index);
                            }
                            else
                            {
                                query = query.Where(p => FilterPredict(p.Name, filter));
                            }

                            var project = query.FirstOrDefault();

                            if (project != null)
                            {
                                wd = GetDirOfProject(project);
                            }
                            else
                            {
                                WriteLine($"No project found for argument: '{filter}'");
                                return 0;
                            }
                        }

                        LaunchVSCmd(wd);

                        return 0;
                    });
                })
            );
        }
    }
}
