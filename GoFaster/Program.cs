using GoFaster.Commands;
using GoFaster.Models;
using GoFaster.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GoFaster
{
    class Program
    {
        static int Main(string[] args)
        {
            var context = new GoFasterContext();

            if (args.Contains("-i", StringComparer.OrdinalIgnoreCase)
                || args.Contains("--interactive", StringComparer.OrdinalIgnoreCase))
            {
                //bool printArrow = true;
                while (true)
                {
                    CommandLineApplication app = CreateCommandLineApp(context);
                    //if (printArrow) 
                    Console.Write("> ");
                    var input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                    {
                        //printArrow = false; 
                        continue;
                    }

                    //printArrow = true;

                    var args2 = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    try
                    {
                        app.Execute(args2);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                CommandLineApplication app = CreateCommandLineApp(context);
                var result = app.Execute(args);

                return result;
            }
        }

        private static CommandLineApplication CreateCommandLineApp(GoFasterContext context)
        {
            var app = new CommandLineApplication();
            app.Name = "GoFaster command tool";
            app.VersionOption("--version -v", typeof(Program).GetTypeInfo().Assembly.GetName().Version.ToString());

            app.FullName = "GoFaster command tool - Shawn Lin";
            app.Description = "GoFaster would make you save time in daily work, type what you want and go...";
            app.Option("-i|--interactive", "enter interactive mode", CommandOptionType.SingleValue);

            app.Argument("<PROJECT_NAME>", "Project Name, can be partial or a regular expression");

            app.HelpOption("-h|--help");

            InitCommands(app, context);

            return app;
        }

        private static void InitCommands(CommandLineApplication app, GoFasterContext context)
        {
            ////basic
            //new ExitCommand(context).Register(app);
            //new HelpCommand(context).Register(app);
            //new ClearCommand(context).Register(app);

            ////funcations
            //new ListCommand(context).Register(app);
            //new DescribeCommand(context).Register(app);

            var types = Assembly.GetExecutingAssembly().GetTypes().AsQueryable();

            types = types.Where(t => typeof(BaseCommand).IsAssignableFrom(t) && !t.IsAbstract);

            types.ToList().ForEach(t =>
            {
                (Activator.CreateInstance(t, context) as BaseCommand).Register(app);
            });
        }
    }
}
