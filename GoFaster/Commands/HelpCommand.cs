using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class HelpCommand : BaseCommand
    {
        public string Name { get; set; } = "list";

        public HelpCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "?" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "help";

                    c.OnExecute(() =>
                    {
                        app.ShowHelp();
                        return 0;
                    });
                })
            );
        }
    }
}
