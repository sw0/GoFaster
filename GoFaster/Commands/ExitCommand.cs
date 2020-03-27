using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class ExitCommand : BaseCommand
    {
        public string Name { get; set; } = "list";

        public ExitCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "exit", "q", "esc" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "exit GoFaster interactive mode";

                    c.OnExecute(() =>
                    {
                        Environment.Exit(0);
                        return 0;
                    });
                })
            );
        }
    }
}
