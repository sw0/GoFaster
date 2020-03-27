using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class ClearCommand : BaseCommand
    {
        public string Name { get; set; } = "list";

        public ClearCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "clear", "cls" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "clear the console";

                    c.OnExecute(() =>
                    {
                        Console.Clear();
                        return 0;
                    });
                })
            );
        }
    }
}
