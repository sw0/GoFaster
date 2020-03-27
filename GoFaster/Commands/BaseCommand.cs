using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoFaster.Commands
{
    public abstract class BaseCommand
    {
        protected GoFasterContext Context { get; }

        protected BaseCommand(GoFasterContext context)
        {
            Context = context;
        }

        public abstract void Register(CommandLineApplication app);

        protected bool FilterPredict(string value, string filter) {
            if (string.IsNullOrEmpty(filter)) throw new ArgumentException(nameof(filter));
            if (string.IsNullOrEmpty(value)) return false;

            Regex regex = new Regex(filter, RegexOptions.IgnoreCase);

            if (regex.IsMatch(value)) return true;

            return false;
        }

        protected bool FilterPredict<T>(T o, Func<T, string> p, string filter)
        {
            var value = p(o);

            if (string.IsNullOrEmpty(filter)) throw new ArgumentException(nameof(filter));
            if (string.IsNullOrEmpty(value)) return false;

            Regex regex = new Regex(filter, RegexOptions.IgnoreCase);

            if (regex.IsMatch(value)) return true;

            return false;
        }

        protected bool FilterPredict<T>(T o, Func<T, IEnumerable<string>> func, string filter)
        {
            var values = func(o);

            if (string.IsNullOrEmpty(filter)) throw new ArgumentException(nameof(filter));

            if (values == null || values.Count() == 0) return false;

            Regex regex = new Regex(filter, RegexOptions.IgnoreCase);

            if (values.Any(v => regex.IsMatch(v))) return true;

            return true;
        }
    }
}
