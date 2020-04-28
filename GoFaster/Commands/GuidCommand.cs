using GoFaster.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public class GuidCommand : BaseCommand
    {
        public string Name { get; set; } = "list";

        public GuidCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "guid", "uuid" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "Generate new GUID and set in clipboard";

                    c.OnExecute(() =>
                    {
                        WriteLine($"Guid generated: {Guid.NewGuid()}");
                        return 0;
                    });
                })
            );
        }
    }
}
