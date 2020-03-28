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
    public class PingCommand : BaseCommand
    {
        public PingCommand(GoFasterContext context) : base(context)
        {
        }

        public override void Register(CommandLineApplication app)
        {
            new[] { "ping" }.ToList()
                .ForEach(name =>
                app.Command(name, c =>
                {
                    c.Description = "sync the code from SCM)";

                    var arg = c.Argument("<DOMAIN>", "domain name");

                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        if (!c.Options.Any(o => o.HasValue())
                            && string.IsNullOrEmpty(arg.Value))
                        {
                            c.ShowHelp();
                        }

                        var domain = arg.Value;
                        var s = domain.Ping();

                        WriteLine(s);

                        return 0;
                    });
                })
            );
        }
    }
}
