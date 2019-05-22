using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Slin.GoFaster
{
    class Program
    {
        /**
         * 0.2.0.0  initial version; TODO process `CmdEntry`
         * 0.3.0.0  rename P4Cmd to GoFaster and use gf as assembly name
         * 0.4.0.0  process `CmdEntry`, introduce uuid, support > cmd {project}; TODO to support lscmd
         * 0.5.0.0  improvement and bug fixing
         * 1.0.0.0  require administrator priviliages by introducing app.manifest.
         * 2.0.0.0  support searching in category, name with regular expression
         * 2.0.0.1  support vs|vs{\d4} command to launch VS
         * 2.0.0.2  support launch visual studio command: vscmd
         * 2.0.2.0  support to set default wiki url in configuration
         * 2.0.3.0  support pattern for name in sync,bld,fld and other commands
         * */
        const string AppName = "GoFaster";
        const string AppVersion = "2.0.0.2";
        private static string CmdRegularExpressionString;
        static Regex RegBranch;
        static readonly Regex RegArgs = new Regex(@"/?\b(?<optkey>[a-zA-Z]+)[\:|=](?<optval>[^""\s]+|""(?:[^""]+""))|-(?<optval>[a-zA-Z]+)\s+(?<optval>[^""\s]+|""(?:[^""]+)"")|--(?<optflag>[a-zA-Z]+)");
        static string _p4Workspace = @"c:\p4";  // my personal default perforce workspace
        static string _wcfTestClientLocation = @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\WcfTestClient.exe";

        static readonly Regex _regAction = null;
        static readonly Regex _regP4Workspace = new Regex(@"^c:\\P4", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex _regP4Config = new Regex(@"REM\s+--START\s*P4(.+)REM --END P4 CONFIG--", RegexOptions.Compiled | RegexOptions.Singleline);

        static List<string> VSExes = null;

        static string _workingBranch;
        static string DefaultWorkingBranch;
        static string _defaultTeams = string.Empty; //or "all"
        static List<Project> AllProjects = new List<Project>();
        static List<Project> CurrentProjects = new List<Project>();
        static List<CmdEntry> CmdEntries = new List<CmdEntry>();

        static readonly string AllCommandNamesNoNeedProject = ",list,ls,lscmd,cmd,mmc,ping,eventviewer,notepad,notepad++,desc,describe,wiki,p4v,inetmgr,ssms,sql,postman,pm,iisreset,help,?,set,db,hosts,folder,fld,code,wcf,uuid,guid,vs,donate,";
        const string FolderHost = @"C:\Windows\System32\drivers\etc\";
        internal static readonly Dictionary<string, string> KeyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal static readonly Dictionary<string, string> ValueMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal static readonly Dictionary<string, string> HostsRepositories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal static Dictionary<string, string> CmdSamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static bool _isFirstRun = false;
        static bool _enableLog = false;
        private static string CurrentCommand;
        const int ColumnSize = 36;  // Console.WindowSize / _columnSize to get the column count
        const string ProfileFileName = "profile.xml";
        const string SyncCfgFileName = "sync_cfg.cmd";
        const string SyncCmdSampleFileName = "sync_sample.txt";
        const string SyncCmdFileName = "sync.cmd";
        const string ManualFileName = "manual.xml";
        private const int Indent = 2;
        static readonly char[] _commaSpaceSeparater = new[] { ',', ' ' };
        static readonly char[] _spaceSeparater = new[] { ' ' };

        //TODO using donation link? or using paypal.me?
        static string PPDonationLink = @"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=LINSW%40LIVE.CN&lc=&no_note=0&item_name=support+Go+Faster+developer&cn=&currency_code=&bn=PP-DonationsBF:btn_donateCC_LG.gif:NonHosted";

        static Program()
        {
            CmdRegularExpressionString = $@"^\s*(?<command>sync|open|bld|build|start|folder|fld|code|url|wiki|cmd|desc|describe)\b(?:\s+(?<projNoOrName>[\^?\._\w]+\$?))?"
            + $@"|^\s*(?<command>(?:list|ls|set)\b)\s*"  //e.g. list /team:team8 /name:coreapi /category:ecash
            + $@"|^\s*(?<command>notepad|notepad\+\+|p4v|inetmgr|ssms|sql|iisreset|vs\d{4}|wcf|postman|pm)\b\s*"
            + @"|^\s*(?<command>help\b|\?)\s*$"
            + @"|^\s*(?<command>mmc|eventviewer)\b\s*$"
            + @"|^\s*(?<command>uuid|guid)\b"
            + @"|^\s*(?<command>vs\d{4}|vs|vscmd)\s*$"
            + @"|^\s*(?<command>donate)\b"
            + @"|^\s*(?<command>ping)\s+(?<action>.+)\s*$"  //action actually is IP or host name here
            + $@"|^\s*(?<command>hosts)\b(?:\s+(?<action>open|set|find|restore|fld|folder))?\s*"  //host, env, for:
            + @"|^\s*(?<command>db)\b(?:\s+(?<dbName>[-\w]+))?";  //set branch=int;

            _regAction = new Regex(CmdRegularExpressionString,
                RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

            InitParametersMappings();
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 &&
                (args[0].ToLower() == "help" || args[0].ToLower() == "/help" || args[0].ToLower() == "--help"
                || args[0].ToLower() == "?" || args[0].ToLower() == "/?"))
            {
                Help();
                Console.ReadKey();
            }
            var options = ResolveOptions(null, null, args);
            if (options.ContainsKey("enablelog") || options.ContainsKey("dbg") || options.ContainsKey("debug")) _enableLog = true;

            if (_enableLog) Console.WriteLine(CmdRegularExpressionString);

            InitConfigurations();

            Console.WriteLine("Please run this as administrator(some projects needs it to setup IIS website/application. CTRL+Shift+Enter)");
            //Console.WriteLine("Files you probably need to maintain are: projects.xml and sync.cmd.");
            if (_enableLog) Console.WriteLine($"DEBUG: {ConfigurationManager.AppSettings["P4Client"]}");

            if (_isFirstRun || options.ContainsKey("init"))
            {
                SetupSampleProfile(options);

                if (_isFirstRun)
                {
                    WriteLine();
                    Help(); WriteLine("\r\nPress any key to list all the projects.");
                    Console.ReadKey();
                }
            }

            var profile = GetProfile();
            var allProjects = profile.Projects;
            AllProjects = allProjects.Where(p => p.Enabled).ToList();
            CurrentProjects = AllProjects.OrderBy(p => p.Name).ToList();

            CmdEntries = profile.CmdEntries ?? new List<CmdEntry>();

            WriteLineIdtIf(allProjects.Count == 0 || AllProjects.Count == 0, "invalid projects.xml file or no enabled projects in it.");
            Console.WriteLine($"Got {CurrentProjects.Count} projects loaded from projects.xml; p4 sync cmd got maintained in sync.cmd.");

            if (_enableLog) foreach (var kvp in options) Console.WriteLine($"OPTION: {kvp.Key} : {kvp.Value}");
            _workingBranch = options.ContainsKey("branch") ? options["branch"] : DefaultWorkingBranch;
            Console.WriteLine($"\r\nFollowing are the projects for working branch '{_workingBranch}':");

            SetBranch(AllProjects, _workingBranch);

            ListProjects(options);

            Console.WriteLine();
        STEP01:
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("> 'sync|open|build|bld|desc|folder|fld|code|url <projNoOrName> [b:<branch>]';");
            WriteLineIdt($"'hosts open/set [e[nv]:local|di|dev-int|qa4|qa3[-nonasm]|QA|STG] [for:default|repo1|repo2|move]'");
            WriteLineIdt($"'wiki|notepad(++)|p4v|inetmgr|ssms|sql|mmc|cmd|postman|pm|iisreset|list|ls|wcf' for help, run p4v, list projects");
            WriteLineIdt($"'?' or '<cmd> ?' for help. 'exit|quit|q|cls|clear' to quit/clear. Or try 'donate' if you like this tool :) Thanks!");
            Console.ForegroundColor = old;

        STEP02:
            if (Console.CursorLeft > 0) Console.WriteLine();
            Console.Write("> ");
            //todo autocomplete

            var inputCmd = Console.ReadLine() ?? string.Empty;

            if (",q,exit,quit,".IndexOf("," + inputCmd.ToLower() + ",", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.Clear();
                //do nothing and exit
            }
            else if (inputCmd.ToLower() == "cls" || "clear" == inputCmd.ToLower())
            {
                Console.Clear();
                goto STEP01;
            }
            else
            {
                var matchAction = _regAction.Match(inputCmd);
                if (matchAction.Success)
                {
                    CurrentCommand = inputCmd;

                    var command = matchAction.Groups["command"].Value.ToLower();
                    var action = matchAction.Groups["action"].Value;
                    var projNoOrName = matchAction.Groups["projNoOrName"].Value;

                    if (string.IsNullOrEmpty(command) && !string.IsNullOrEmpty(projNoOrName))
                        command = "open";

                    var matchedCmd = matchAction.ToString();
                    var optionsString = inputCmd.Substring(matchedCmd.Length).Trim();
                    var parameters = ResolveOptions(command, action, optionsString.Split(_spaceSeparater, StringSplitOptions.RemoveEmptyEntries));

                    if (_enableLog)
                        Console.WriteLine($"STEP02:: command: {command}, action: {action}, projNoOrName: {projNoOrName}, default teams: {_defaultTeams} \r\nparameters: {string.Join(", ", parameters.Select(o => $"{o.Key}:{o.Value}").ToList())}");
                    if (",help,?,--help,".Contains($",{optionsString},".ToLower()))
                    {
                        Help(command, null);
                        goto STEP02;
                    }

                    GoAction(command, action, projNoOrName, AllProjects, parameters);

                    if (",cls,clear,".IndexOf("," + command + ",", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        goto STEP01;
                    }

                    goto STEP02;
                }

                if (!string.IsNullOrWhiteSpace(inputCmd) && inputCmd.StartsWith(":") && CmdEntries != null)
                {
                    ProcessCmdEntry(inputCmd, true);
                    goto STEP02;
                }
                else if (!string.IsNullOrWhiteSpace(inputCmd)) // cmd starts with ':' means custom entry
                {
                    var arr = inputCmd.Split(_spaceSeparater, StringSplitOptions.RemoveEmptyEntries);
                    var firstSegment = arr[0];
                    var secondSeg = (arr.Length > 1) ? arr[1].ToLower() : "";
                    if (CmdSamples.ContainsKey(firstSegment))
                    {
                        if (secondSeg != "help" && secondSeg != "?" && arr.Length > 1) WriteLineIdt("Invalid input, examples: ");
                        old = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        CmdSamples[firstSegment].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList().ForEach(eg => WriteLineIdt("* " + eg.Trim()));
                        Console.ForegroundColor = old;
                        goto STEP02;
                    }

                    var parameters = ResolveOptions("open", null, inputCmd.Split(_spaceSeparater, StringSplitOptions.RemoveEmptyEntries));
                    if (GoAction("open", null, inputCmd.Trim(), CurrentProjects, parameters) == null)
                    {
                        ProcessCmdEntry(inputCmd, false);
                    }
                    goto STEP02;
                }

                WriteLineIdt("invalid input, try again");
                goto STEP02;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputCmd"></param>
        /// <param name="showFailMsg"></param>
        /// <returns>means cmd found in profile</returns>
        static bool ProcessCmdEntry(string inputCmd, bool showFailMsg = true)
        {
            if (inputCmd == null) return false;

            inputCmd = inputCmd.TrimStart(':');

            var arr = inputCmd.Split(_spaceSeparater, 2, StringSplitOptions.RemoveEmptyEntries);
            var isCmdFound = false;
            if (arr.Length > 0)
            {
                var cmd = arr[0].ToLower();
                var args = arr.Length > 1 ? arr[1] : (string)null;
                var found = CmdEntries.FirstOrDefault(entry => entry != null && entry.CmdName != null
                && entry.CmdName.Split(new[] { ',' }).Contains(cmd));

                if (found != null)
                {
                    isCmdFound = true;
                    try
                    {
                        args = args ?? found.CmdArgs;
                        WriteLineIdt($"process cmd: {found.Process} {args}");
                        Process.Start(found.Process, args ?? found.CmdArgs);
                    }
                    catch (Exception ex)
                    {
                        WriteLineIdtIf(showFailMsg, $"error occurred when process: {inputCmd}: {ex.Message}");
                    }
                }
                else if (showFailMsg)
                {
                    Console.WriteLine($"command '{cmd}' not found in profile file. please check it.");
                }
            }
            return isCmdFound;
        }

        private static void InitConfigurations()
        {
            _p4Workspace = ConfigurationManager.AppSettings["P4Workspace"]?.TrimEnd('\\').Replace('/', '\\');
            _wcfTestClientLocation = ConfigurationManager.AppSettings["WcfTestClientLocation"]?.ToString();
            DefaultWorkingBranch = ConfigurationManager.AppSettings["DefaultWorkingBranch"]?.ToString() ?? "current";
            var branchRegexPattern = ConfigurationManager.AppSettings["BranchRegexPattern"]?.ToString();
            if (string.IsNullOrWhiteSpace(branchRegexPattern)) throw new Exception("appSetting for BranchRegexPattern must be set!");

            RegBranch = new Regex(branchRegexPattern);
            if (Assembly.GetExecutingAssembly() != null)
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            #region --read manual.xml--
            try
            {
                var xdoc = XDocument.Load(ManualFileName);
                xdoc.Root.Elements().ToList().ForEach(node =>
                {
                    var cmdArr = node.Element("command").Value?.Trim()?.ToLower()
                    ?.Split(_commaSpaceSeparater, StringSplitOptions.RemoveEmptyEntries);
                    var sample = node.Element("sample").Value?.Trim();
                    if (cmdArr != null && cmdArr.Length > 0 && sample != null)
                    {
                        cmdArr.ToList().ForEach(cmd =>
                        {
                            if (CmdSamples.ContainsKey(cmd)) return;
                            CmdSamples[cmd] = sample;
                        });
                    }
                });
            }
            catch (Exception ex) { WriteLine($"Error occurred in handling manual file: {ex.Message}"); }
            #endregion

            #region --sync_init.cmd--
            var nl = Environment.NewLine;
            var initSyncInitContent = $"@echo off{nl}{nl}REM --START P4 CONFIG--{nl}"
                + $"#PARAMETERS WILL BE SET BASE ON CONFIGURATION HERE{nl}"
                + $"REM --END P4 CONFIG--{nl}echo P4Port:   %P4Port%{nl}echo P4USER:   %P4USER%{nl}echo P4Client: %P4Client%{nl}";

            var p4Port = ConfigurationManager.AppSettings["P4Port"];
            var p4Client = ConfigurationManager.AppSettings["P4Client"];
            var p4User = ConfigurationManager.AppSettings["P4User"];
            _defaultTeams = ConfigurationManager.AppSettings["DefaultTeams"];
            //_enableLog maybe set by command argument already
            if (!_enableLog) bool.TryParse(ConfigurationManager.AppSettings["EnableLog"], out _enableLog);

            //update sync_init.cmd base on user's environment
            var realConfig = $@"REM --START P4 CONFIG--{nl}set P4Port={p4Port}{nl}set P4Client={p4Client}{nl}SET P4USER={p4User}{nl}SET P4WorkspaceMappedPath={_p4Workspace}{nl}REM --END P4 CONFIG--{nl}";

            if (File.Exists(SyncCfgFileName))
            {
                var syncInitAttrs = File.GetAttributes(SyncCfgFileName);
                if ((syncInitAttrs & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(SyncCfgFileName, syncInitAttrs ^ FileAttributes.ReadOnly);
                }
                File.WriteAllText(SyncCfgFileName, _regP4Config.Replace(File.ReadAllText(SyncCfgFileName), realConfig));
            }
            else
            {
                var content = _regP4Config.Replace(initSyncInitContent, realConfig);
                File.WriteAllText(SyncCfgFileName, content);
                _isFirstRun = true;
            }
            //END sync_init.cmd

            #endregion

            #region -- hosts repositories --
            //configuration hosts
            var tmp = ConfigurationManager.AppSettings["HostsRepositories"];
            if (!string.IsNullOrWhiteSpace(tmp))
            {
                var entries = tmp.Split(new[] { ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                HostsRepositories.Add("default(local)", $@"{FolderHost}hosts");
                //hosts-mine: your customized hosts file
                HostsRepositories.Add("default(mine)", $@"{FolderHost}hosts-mine");

                entries.ForEach(entry =>
                {
                    var row = entry.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (row.Length >= 3)
                    {
                        var name = row[0];
                        var dirSuffix = row[1];
                        var environments = row[2].Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
                        //Console.WriteLine($".. {dirSuffix}");
                        const char slash = '\\';
                        foreach (var env in environments)
                        {
                            var dir = _p4Workspace.TrimEnd(slash) + slash + dirSuffix.Trim(slash) + slash + env + "\\hosts";
                            if (name == "default")
                            {
                                HostsRepositories.Add($"{name}({env})".ToLower(), dir + "-ASM");
                                HostsRepositories.Add($"{name}({env}-nonasm)".ToLower(), dir + "-NonASM");
                            }
                            else
                            {
                                HostsRepositories.Add($"{name}({env})".ToLower(), dir);
                            }
                        }

                        if (_enableLog)
                        {
                            foreach (var kvp in HostsRepositories)
                            {
                                WriteLineIdt($"HostsRepositories > {kvp.Key} : {kvp.Value}");
                            }
                        }
                    }
                });
            }

            #endregion

            #region -- find all visual studio installed --
            {
                var vsBaseDir = @"C:\Program Files (x86)\Microsoft Visual Studio\";
                var subDirs = Directory.GetDirectories(vsBaseDir, "20*");
                var versions = new[] { "Enterprise", "Professional", "Community" };
                //todo cache these exe locations
                VSExes = subDirs.ToList().SelectMany(dir =>
                {
                    var exeList = new List<string>();
                    foreach (var ver in versions)
                    {
                        var exefile = dir + $@"\{ver}\Common7\IDE\devenv.exe";
                        if (File.Exists(exefile))
                            exeList.Add(exefile);
                    }
                    return exeList;
                }).ToList();
            }
            #endregion
        }

        private static void ListProjects(IDictionary<string, string> parameters)
        {
            var owners = (parameters.ContainsKey("team") ? parameters["team"] : _defaultTeams)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var category = parameters.ContainsKey("category") ? parameters["category"] : null;
            var name = parameters.ContainsKey("name") ? parameters["name"] : null;

            owners = owners ?? new string[0];
            CurrentProjects = AllProjects.Where(p => owners.Length == 0
               || owners.Contains("all", StringComparer.OrdinalIgnoreCase)
                || owners.Any(owner => TryIsMatch(p.Owner, owner))).ToList();

            CurrentProjects = CurrentProjects.Where(p => string.IsNullOrEmpty(category)
                || p.Category.Split(_commaSpaceSeparater, StringSplitOptions.RemoveEmptyEntries)
                    .Any(c => TryIsMatch(c, category, RegexOptions.IgnoreCase))).ToList();

            var list = CurrentProjects;
            if (!string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    list = list.Where(p => name == "*" || TryIsMatch(p.Name, name)).ToList();
                }
                catch (Exception ex)
                {
                    WriteLineIdt($"option name '{name}' is in bad format: {ex.Message}"); return;
                }
            }

            if (_enableLog)
                Console.WriteLine($"AllProjects: {AllProjects.Count}, CurrentProjects: {CurrentProjects.Count}, list:  {list.Count}, owner: {String.Join(",", owners)}, category: {category}, name: {name}");

            var groups = list.GroupBy(item => item.Owner.ToUpper());
            if (groups.Count() == 0 && !string.IsNullOrEmpty(name))
            {
                WriteLineIdt($"cannot find the project with name matching pattern '{name}'");
                return;
            }

            foreach (var g in groups.OrderBy(g => g.Key))
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                if (Console.CursorLeft > 0 && Console.CursorLeft != Console.WindowWidth) Console.WriteLine();
                Console.WriteLine($"[TEAM/OWNER: {g.Key}]");//  {Console.WindowWidth} {Console.CursorLeft}");
                Console.ForegroundColor = old;
                foreach (var item in g)
                {
                    //var printName = $"{item.Index, 3:d} {item.Name}".PadRight(_columnSize).Substring(0, _columnSize);
                    var printName = $"{item.Index:d3} {item.Name}".PadRight(ColumnSize).Substring(0, ColumnSize);
                    Console.Write(printName);

                    if (Console.WindowWidth - Console.CursorLeft < ColumnSize)
                        Console.WriteLine();
                }
            }
            Console.WriteLine();
        }

        #region Console Help Methods
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
        #endregion

        private static void SetBranch(List<Project> projects, string branchNumber)
        {
            var title = Console.Title;
            var idx = title.IndexOf($" - {AppName}", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                Console.Title = $"{title.Substring(0, idx)} - {AppName} on branch {branchNumber} (V{AppVersion})";
            }
            else
            {
                Console.Title = $"{title} - {AppName} on branch {branchNumber} (V{AppVersion})";
            }

            for (int i = 0; i < projects.Count; i++)
            {
                projects[i].Path = RegBranch.Replace(projects[i].Path, "\\" + branchNumber + "\\");
                projects[i].Entry = RegBranch.Replace(projects[i].Entry, "\\" + branchNumber + "\\");
            }
        }

        #region --Serialization--
        static void SerializeToXml<T>(T obj, string path = @".\projects.xml") where T : class
        {
            if (obj == null) return;
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

                xmlSerializer.Serialize(sw, obj);
            }
        }
        static T Deserialize<T>(string path = @".\Projects.xml") where T : class
        {
            using (Stream strm = File.OpenRead(path))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                var result = xmlSerializer.Deserialize(strm) as T;
                return result;
            }
        }
        #endregion

        static Project GetProj(string projName)
        {
            var proj = CurrentProjects.FirstOrDefault(p => p.Name.Equals(projName, StringComparison.OrdinalIgnoreCase))
            ?? CurrentProjects.FirstOrDefault(p => TryIsMatch(p.Name, projName));

            return proj;
        }

        #region --Action--
        static Project GoAction(string command, string action, string projNoOrName,
            List<Project> projects,
            Dictionary<string, string> parameters)
        {
            command = command.ToLower();
            var branchName = string.Empty;
            parameters.TryGetValue("branch", out branchName);
            branchName = branchName ?? _workingBranch;
            if (_enableLog) Console.WriteLine($"command: {command} proj: {projNoOrName} branch: {branchName} , {string.Join(",", parameters.Select(kv => $"{kv.Key}:{kv.Value}"))}");
            parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            projNoOrName = projNoOrName ?? string.Empty;
            Project project = null;

            if (!string.IsNullOrEmpty(projNoOrName))
            {
                if (projNoOrName.All(c => char.IsNumber(c)))
                {
                    project = AllProjects.FirstOrDefault(o => o.Index == Convert.ToInt32(projNoOrName)); //GetProj(Convert.ToInt32(projNoOrName));
                }
                else
                {
                    project = GetProj(projNoOrName);
                }
            }

            if (project == null
                && (AllCommandNamesNoNeedProject.IndexOf($",{command},", StringComparison.OrdinalIgnoreCase) == -1
                && !Regex.IsMatch(command, "^vs|vs\\d{4}$")))
            {
                WriteLineIdt($"command '{command}' needs project name or number.{(string.IsNullOrEmpty(projNoOrName) ? "" : $" but no project found with pattern '{projNoOrName}' for name.")}");
                return project;
            }

            try
            {
                if (command.Equals("open", StringComparison.OrdinalIgnoreCase))
                {
                    Open(project, branchName, parameters);
                }
                else if (command.Equals("start", StringComparison.OrdinalIgnoreCase))
                {
                    Start(project, parameters.ContainsKey("host") ? parameters["host"] : string.Empty);
                }
                else if (command.Equals("db", StringComparison.OrdinalIgnoreCase))
                {
                    try { DbInfo(parameters.ContainsKey("dbName") ? parameters["dbName"] : null); }
                    catch (Exception exx98) { WriteLineIdt(exx98.Message); }
                }
                else if (command.Equals("cmd", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessCmdCmd(project, branchName, parameters);
                }
                else if (command.Equals("wiki", StringComparison.OrdinalIgnoreCase))
                {
                    OpenWiki(project, parameters);
                }
                else if (command.Equals("p4v", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start("p4v");
                }
                else if (command.StartsWith("notepad", StringComparison.OrdinalIgnoreCase))
                {
                    var npProcess = command.EndsWith("++") ? @"C:\Program Files (x86)\Notepad++\notepad++.exe" : "notepad.exe";
                    Process.Start(npProcess);
                }
                else if (command.Equals("wcf", StringComparison.OrdinalIgnoreCase) || command.Equals("wcftc", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(_wcfTestClientLocation);
                }
                else if (command.Equals("inetmgr", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(@"C:\Windows\System32\inetsrv\inetmgr.exe");
                }
                else if (command.Equals("mmc", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(@"C:\Windows\System32\mmc.exe");
                }
                else if (command.Equals("eventviewer", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(@"eventvwr.msc", "/s");
                }
                else if (command.Equals("Ssms", StringComparison.OrdinalIgnoreCase) || command.Equals("sql", StringComparison.OrdinalIgnoreCase))
                {
                    var loc = ConfigurationManager.AppSettings["SsmsLocation"];
                    loc = string.IsNullOrEmpty(loc) ? @"C:\Program Files (x86)\Microsoft SQL Server\100\Tools\Binn\VSShell\Common7\IDE\Ssms.exe" : loc;
                    try
                    {
                        Process.Start(loc);
                    }
                    catch { WriteLineIdt($"Failed to run {loc}"); }
                }
                else if (command.Equals("ping", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO ping
                    var sCmdText = $@"ping {action}";
                    var p = new Process();
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = sCmdText;
                    p.StartInfo.RedirectStandardOutput = false;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                }
                else if (command.Equals("hosts", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessHostsCmd(action, parameters);
                }
                else if (command.Equals("iisreset", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start("iisreset.exe");
                }
                else if ((new[] { "postman", "pm" }).Contains(command.ToLower()))
                {
                    StartPostman();
                }
                else if (",list,ls,".Contains(string.Concat(",", command, ",")))
                {
                    ListProjects(parameters);
                }
                else if (command.Equals("set", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessSetCmd(project, parameters);
                }
                else if (command.Equals("help", StringComparison.OrdinalIgnoreCase) || "?" == command)
                {
                    Help();
                }
                else if (command.Equals("build", StringComparison.OrdinalIgnoreCase) || command.Equals("bld", StringComparison.OrdinalIgnoreCase))
                {
                    Build(project, branchName);
                }
                else if (command.Equals("folder", StringComparison.OrdinalIgnoreCase) || command.Equals("fld", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessFolderCmd(project, branchName, parameters);
                }
                else if (command.Equals("code", StringComparison.OrdinalIgnoreCase))
                {
                    CodeFolder(project, branchName);
                }
                else if (command.Equals("sync", StringComparison.OrdinalIgnoreCase))
                {
                    Sync(project, branchName, parameters.ContainsKey("f") || parameters.ContainsKey("force"));
                }
                else if (command.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessUrlCmd(project, parameters);
                }
                else if ((new[] { "uuid", "guid" }).Contains(command))
                {
                    var guid = Guid.NewGuid().ToString();
                    Clipboard.SetText(guid); WriteLineIdt($"guid copied to clipboard: {guid}");
                }
                else if ("vscmd" == command)
                {
                    LaunchVSCmd(command, parameters);
                }
                else if (Regex.IsMatch(command, "^vs(?:\\d{4})?$"))
                {
                    LaunchVS(command, parameters);
                }
                else if ("donate" == command)
                {
                    Process.Start(PPDonationLink);
                }
                else if ((new[] { "desc", "describe" }).Contains(command))
                {
                    if (project == null)
                    {
                        projects.ForEach(p => Console.WriteLine(p.ToDescriptionString(branchName)));
                    }
                    else
                    {
                        Console.WriteLine(project.ToDescriptionString(branchName));
                    }
                }
                else
                {
                    WriteLineIdt($"the command '{command}' is not supported yet");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                WriteLineIdt($"Error: {ex.Message}");
                WriteLineIdt("Reminder: please check whether you run this as administrator mode");
                Console.ResetColor();
            }

            return project;
        }

        static void LaunchVSCmd(string command, Dictionary<string, string> parameters) {
            //%comspec% /k "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"
            var vsExe = VSExes.FirstOrDefault();
            var cmdBat = string.Empty;
            if (vsExe != null && File.Exists(cmdBat = vsExe.Replace("IDE\\devenv.exe", "Tools\\VsDevCmd.bat"))) {
                var wd = Path.GetFullPath(vsExe).ToLower();
                var p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WorkingDirectory = wd.Substring(0, wd.IndexOf("\\common"));
                p.StartInfo.Arguments = $"/k \"{cmdBat}\"";
                p.StartInfo.RedirectStandardOutput = false;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
        }
        static void LaunchVS(string command, Dictionary<string, string> parameters)
        {
            var verInCmd = (string)null;
            var vsExe2Run = (string)null;
            var givenVersionNotFound = command.Length == 6;
            if (VSExes.Count > 0 && command.Length == 6
                && VSExes.Any(s => s.Contains(verInCmd = command.Substring(2))))//contains vs version
            {
                vsExe2Run = VSExes.FirstOrDefault(s => s.Contains(verInCmd));
                givenVersionNotFound = false;
            }
            vsExe2Run = vsExe2Run ?? VSExes.FirstOrDefault();
            if (givenVersionNotFound || vsExe2Run == null)
            {
                Console.WriteLine("given version of VS not found");
            }
            if (!string.IsNullOrEmpty(vsExe2Run))
            {
                Process.Start(vsExe2Run);
            }
        }

        static void ProcessHostsCmd(string action, Dictionary<string, string> parameters)
        {
            /**
             * NOTE: I want to merge all the hosts file for given branch/environment
             * > hosts set env:dev-int
             * > hosts set for:repo1,repo2,repo3 env:DEV merge:dev-int (default)
             */
            var repositories = new List<string>();
            var env = string.Empty;
            if (parameters.TryGetValue("for", out var forStr))
            {
                repositories = forStr.ToLower().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(row => row != "default").Distinct().ToList();
            }
            if (repositories.Count == 0) repositories.Add("default");

            if (!parameters.TryGetValue("environment", out env) && !parameters.TryGetValue("branch", out env))
                env = repositories.FirstOrDefault() == "default" ? "local" : "DEV"; //'DEV' for SDLC, repo1, repo2

            if (repositories.All(row => row != "default") && env == "integration") env = "INT"; //FOR SDLC, repo1, repo2, folder is INT for integration branch

            if (string.IsNullOrEmpty(action)) action = "open";
            action = action.ToLower();
            if (action == "fld") action = "folder";

            var dicKey = $"{repositories.FirstOrDefault()}({env})".ToLower();

            if (action == "open" || action == "set" || action == "find" || action == "restore" || action == "fld" || action == "folder")
            {
                var file = string.Empty;
                if (!HostsRepositories.TryGetValue(dicKey, out file))
                {
                    var availKeys = HostsRepositories.Keys
                        .Where(k => k.Contains(dicKey.Substring(0, dicKey.IndexOf('('))))
                        .Select(k => k.Substring(k.IndexOf('(')).Trim('(', ')')).ToList();
                    WriteLineIdt($"hosts file not found for key: {dicKey}, try env:{string.Join("|", availKeys)}");
                    return;
                }

                if (action == "open" || action == "folder")
                {
                    if (action == "open" && File.Exists(file))
                    {
                        Process.Start("notepad.exe", file);
                        WriteLineIdt($"open {file}");
                        return;
                    }

                    if (action == "folder")
                    {
                        Process.Start(FolderHost);
                        return;
                    }

                    WriteLineIdt($"file not found: {file}");
                }

                if (action == "set" || action == "restore")
                {
                    if (action == "set" && (!parameters.ContainsKey("environment") && !parameters.ContainsKey("branch")))
                    {
                        WriteLineIdt($"invalid input. example: > hosts set env:QA for:repo1");
                        return;
                    }
                    var localHostFile = HostsRepositories["default(local)"];
                    var attributes = File.GetAttributes(localHostFile);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(localHostFile, attributes ^ FileAttributes.ReadOnly);

                    //var src = file;
                    if (action == "set")
                    {
                        if (repositories.Count > 1)
                        {
                            //merge hosts
                            var defaultEnv2Merge = string.Empty;
                            parameters.TryGetValue("merge", out defaultEnv2Merge);

                            var mergedHostContent = MergeHostEntries(repositories, env, defaultEnv2Merge);

                            File.Copy(localHostFile, localHostFile + DateTime.Now.ToString("_yyyyMMddmmss"));
                            File.WriteAllText(localHostFile, mergedHostContent);
                            WriteLineIdt($"hosts updated with merged hosts from `{string.Join(",", repositories)}{(defaultEnv2Merge == null ? "" : $",default({defaultEnv2Merge})")}` successfully");
                        }
                        else
                        {
                            File.Copy(localHostFile, localHostFile + DateTime.Now.ToString("_yyyyMMddmmss"));
                            File.Copy(file, localHostFile, true);
                            WriteLineIdt($"copied `{file}` successfully");
                        }
                    }
                    else // restore
                    {
                        file = HostsRepositories["default(mine)"];
                        if (!File.Exists(file))
                        {
                            WriteLineIdt($"You did not got custom hosts file set: {file}");
                            return;
                        }
                        File.Copy(file, localHostFile, true);
                        WriteLineIdt($"copied `{file}` successfully");
                    }

                    attributes = File.GetAttributes(localHostFile);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(localHostFile, attributes ^ FileAttributes.ReadOnly);
                }

                if (action == "find")
                {
                    if (parameters.TryGetValue("host", out var host))
                    {
                        var content = File.ReadAllText(file);
                        var regText = $@"^\s*(?<ip>[\d\.]+)\s+{host.Replace(".", "\\.")}.*$|^\s*{host.Replace(".", "\\.")}.+$";
                        var reg = new Regex(regText, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        //Console.WriteLine(regText);
                        var found = false;
                        var m = reg.Match(content);
                        while (m.Success)
                        {
                            var value = Regex.Replace(m.Groups[0].Value.Trim(), @"\s{2,}", "  ");
                            WriteLineIdt($"find `{value}` ({env}) from {file}");
                            m = m.NextMatch();
                            found = true;
                        }

                        if (found) return;
                        WriteLineIdt($"cannot find host entry for {host} in ({env}) from file: {file}");

                        return;
                    }
                    WriteLineIdt("please pass host parameter like host:server1v3 or h:server1");
                }

                return;
            }

            WriteLineIdt("not supported action for command 'hosts'");
        }

        static void ProcessUrlCmd(Project project, Dictionary<string, string> parameters)
        {
            string url = null;
            if (parameters.TryGetValue("type", out var type) && ",tc,teamcity,".Contains($",{type},".ToLower()))
            {
                type = type.ToLower();
                if (type == "tc") type = "teamcity";
                var tmp = project.Endpoints?.FirstOrDefault(end =>
                    type.Equals(end.Type, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(end.Url))?.Url;
                if (!string.IsNullOrEmpty(tmp)) url = tmp;
            }

            url = url ?? project.Url ?? project.Endpoints?.FirstOrDefault(end => !string.IsNullOrEmpty(end.Url))?.Url;

            if (string.IsNullOrEmpty(url))
            {
                WriteLineIdt($"url or endpoints is/are not set for the project: {project?.Name}");
                return;
            }

            if (parameters.ContainsKey("copy"))
            {
                Clipboard.SetText(url);
                WriteLineIdt($"url copied to clipboard: {project?.Name}");
            }
            else
            {
                Process.Start(url);
                return;
            }
        }

        static void ProcessCmdCmd(Project project, string branchName, Dictionary<string, string> parameters)
        {
            if (!string.IsNullOrEmpty(project?.Path))
            {
                var wd = Path.GetDirectoryName(project.Path);
                if (!string.IsNullOrWhiteSpace(branchName)) wd = GetBranchedPath(wd, branchName);
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.WorkingDirectory = wd;
                processStartInfo.FileName = "cmd.exe";
                Process proc = Process.Start(processStartInfo);
            }
            else Process.Start("cmd.exe");
        }
        private static string MergeHostEntries(List<string> repositories, string env, string defaultEnvToMerge)
        {
            var sb = new StringBuilder();

            const int repeatCount = 50;

            sb.Append('-', repeatCount).AppendLine().AppendLine("Generated by following command:");
            sb.Append("  " + CurrentCommand).AppendLine().Append('-', repeatCount).AppendLine("\r\n");

            var files = repositories.Distinct().Select(repository =>
            {
                var dicKey = $"{repository}({env})".ToLower();
                if (HostsRepositories.ContainsKey(dicKey))
                    return HostsRepositories[dicKey];
                return null;
            }).Where(o => o != null).ToDictionary(item => item, item => env);

            if (files.Count > 1)
            {
                files.Add(FolderHost + "hosts-to-append", "my-custom-hosts");
            }

            if (!string.IsNullOrEmpty(defaultEnvToMerge))
            {
                var defaultEnvHost = $"default({defaultEnvToMerge})".ToLower();
                if (HostsRepositories.ContainsKey(defaultEnvHost))
                {
                    if (!files.ContainsKey(HostsRepositories[defaultEnvHost]))
                        files.Add(HostsRepositories[defaultEnvHost], defaultEnvToMerge);
                }
                else
                {
                    WriteLineIdt($"env value {defaultEnvToMerge} for `default` repositories in `merge:` is invalid and got ignored");
                }
            }

            var dic = new Dictionary<string, string>();
            var hosts = new HashSet<string>();
            var regHostEntry = new Regex(@"^\s*([^\s#]+)\s+([^\s]+)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
            foreach (var kvp in files.ToArray().ToList().OrderBy(o => o.Key))
            {
                var file = kvp.Key;
                var envDescription = kvp.Value;

                var fileExists = File.Exists(file);

                if (file.EndsWith("hosts-to-append") && !fileExists) continue;

                sb.AppendLine(new string('#', repeatCount));
                sb.AppendLine(!fileExists ? $"# FILE NOT FOUND: {file}" : $"# {file}");
                sb.AppendLine(new string('#', repeatCount) + Environment.NewLine);
                if (fileExists)
                {
                    var content = File.ReadAllText(file);
                    var m = regHostEntry.Match(content);
                    while (m.Success)
                    {
                        var host = m.Groups[2].Value.ToLower();
                        var row = (m.Groups[1].Value.PadRight(20) + host.PadRight(36)).ToLower();
                        if (hosts.Contains(host))
                        {
                            m = m.NextMatch();

                            continue;
                        }
                        hosts.Add(host);
                        if (!dic.ContainsKey(row))
                        {
                            dic.Add(row, envDescription);

                            sb.AppendLine($"{row}\t#{envDescription}");
                        }

                        m = m.NextMatch();
                    }

                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        static void ProcessSetCmd(Project proj, Dictionary<string, string> parameters)
        {
            if (parameters.Count == 0)
            {
                WriteLineIdt("no parameter set like b:int dbg:1 --dbg");
                return;
            }

            if (parameters.TryGetValue("branch", out string branch))
            {
                if (!RegBranch.IsMatch("\\" + branch + "\\"))
                {
                    WriteLineIdt($"branch `{branch}` is invalid");
                    return;
                }

                _workingBranch = branch;
                SetBranch(CurrentProjects, branch);
            }

            if (parameters.TryGetValue("debug", out string debug))
            {
                _enableLog = debug != "false" && debug != "0";
            }
        }
        private static void Help(string cmd = null, string title = "GF USAGE INFORMATION:")
        {
            if (title != null) //NOTE: only null would not be printed
                Console.WriteLine(title);

            const int CmdLength = 10;

            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            foreach (var row in CmdSamples.Where(k => string.IsNullOrWhiteSpace(cmd) ||
                    k.Key.Equals(cmd, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(kvp => kvp.Key.ToLower()))
            {
                //var keys = row.Key.Split(_commaSpaceSeparater, StringSplitOptions.RemoveEmptyEntries);
                //keys.ToList().ForEach(key => WriteLineIdt($"{key}\t{row.Value}"));
                var lines = row.Value.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (i == 0)
                        WriteLineIdt($"{row.Key.PadRight(CmdLength)}{lines[i].Trim()}");
                    else
                        WriteLineIdt($"{new string(' ', CmdLength)}{lines[i].Trim()}");
                }
            }
            Console.ForegroundColor = old;
        }

        public static string GetBranchedPath(string path, string newBranchName)
        {
            var tmp = RegBranch.Replace(path, "\\" + newBranchName + "\\");
            return tmp;
        }

        static void Open(Project proj, string branchName, Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("for") && "wiki".Equals(parameters["for"].ToLower()))
            {
                OpenWiki(proj, parameters);
            }
            else if (parameters.ContainsKey("for") && "url".Equals(parameters["for"].ToLower()))
            {
                OpenUrl(proj);
            }
            else if (!string.IsNullOrWhiteSpace(proj.Path))
            {
                var projectPath = GetBranchedPath(proj.Path, branchName);
                if (File.Exists(projectPath))
                {
                    WriteLineIdt($"opening {projectPath}");
                    Process.Start(projectPath);
                    return;
                }

                WriteIdt($"file not found: {projectPath}");
                if (Console.CursorLeft > 0 && Console.CursorLeft != Console.WindowWidth) Console.WriteLine();
                WriteLineIdt("please sync the code firstly like 'sync <projNameOrNo> [b:integration]'.");
            }
            else
            {
                WriteLineIdt($"the solution/project path was not set for Project '{proj.Name}'");
            }
        }

        private static void OpenUrl(Project project)
        {
            var url = project.Url ?? project.Endpoints?.FirstOrDefault(end => !string.IsNullOrEmpty(end.Url))?.Url;
            if (!string.IsNullOrEmpty(url)) Process.Start(url);
            else WriteLineIdt($"url or endpoints is/are not set for the project: {project?.Name}");
        }
        private static void OpenWiki(Project project, Dictionary<string, string> parameters)
        {
            if (project == null)
            {
                var wikiUrl = ConfigurationManager.AppSettings["defaultWiki"]?.Trim();
                wikiUrl = string.IsNullOrWhiteSpace(wikiUrl) ? "https://github.com/sw0/GoFaster/blob/master/README.md" : wikiUrl;
                Process.Start(wikiUrl);
                return;
            }

            var url = project.Wiki;
            if (!string.IsNullOrEmpty(url)) Process.Start(url);
            else WriteLineIdt($"wiki page is not set for the project: {project?.Name}");
        }

        static void ProcessFolderCmd(Project proj, string branchName, IDictionary<string, string> parameters)
        {
            var path = string.Empty;
            if (proj == null)
            {
                var codeBase = Assembly.GetEntryAssembly().CodeBase;
                codeBase = Path.GetDirectoryName(codeBase);
                path = codeBase;
            }
            else
            {
                path = proj.Path;

                if (!string.IsNullOrWhiteSpace(proj.Path))
                {
                    path = Path.GetDirectoryName(GetBranchedPath(path, branchName));
                }
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                if (parameters != null && parameters.ContainsKey("copy"))
                {
                    Clipboard.SetText(path);
                    WriteLineIdt($"path copied to clipboard: {path}");
                    return;
                }
                WriteLineIdt($"Opening {path}");
                Process.Start(path);
            }
            else
            {
                WriteLineIdt($"The solution/project path was not set for Project '{proj?.Name}'");
            }
        }

        static void CodeFolder(Project proj, string branchName)
        {
            WriteLineIdt(proj.Path);
            if (!string.IsNullOrWhiteSpace(proj.Path))
            {
                var path = Path.GetDirectoryName(GetBranchedPath(proj.Path, branchName));
                WriteLineIdt($"Opening {path}");
                var p = new Process { StartInfo = { FileName = "code", Arguments = path } };
                p.Start();
            }
            else
            {
                WriteLineIdt($"the solution/project path was not set for Project '{proj.Name}'");
            }
        }

        static void Start(Project proj, string host)
        {
            if (!string.IsNullOrWhiteSpace(proj.Url))
            {
                try
                {
                    Uri u = new Uri(proj.Url);

                    if (!string.IsNullOrEmpty(host))
                    {
                        if (u.Port == 80)
                        {
                            u = new Uri(u.Scheme + "://" + host + u.PathAndQuery);
                        }
                        else
                        {
                            u = new Uri(u.Scheme + ":" + u.Port + "//" + host + u.PathAndQuery);
                        }
                    }

                    WriteLineIdt($"starting {u}");
                    Process.Start(u.AbsoluteUri);
                    return;
                }
                catch { }
            }

            WriteLineIdt($"the solution/project Url was not set for Project '{proj.Name}'");
        }

        static void StartPostman()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Local\Postman\";
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            string pmLocation = null;
            if (Directory.Exists(filePath))
            {
                pmLocation = filePath;

                if (pmLocation != null && File.Exists(pmLocation = pmLocation + "\\update.exe"))
                {
                    try
                    {
                        Process myProcess = new Process();
                        myProcess.StartInfo.Arguments = " --processStart \"Postman.exe\"";
                        myProcess.StartInfo.UseShellExecute = false;
                        myProcess.StartInfo.FileName = pmLocation;
                        myProcess.StartInfo.CreateNoWindow = false;
                        myProcess.EnableRaisingEvents = false;
                        myProcess.Start();
                        myProcess.Close();
                        return;
                    }
                    catch { }
                }
            }
            WriteLineIdt($"Cannot found postman in location: {pmLocation}");
        }

        static void Sync(Project proj, string branchName, bool force)
        {
            if (File.Exists("sync.cmd"))
            {
                WriteLineIdt($"call sync.cmd {proj.Name} {branchName}, force: {force.ToString().ToLower()}");
                SyncCode("sync.cmd", proj.Name, branchName, force);
            }
            else
            {
                WriteLineIdt($"Batch file did not set for Project '{proj.Name}'");
            }
        }
        static void Build(Project proj, string branchName)
        {
            if (proj != null)
            {
                if (!string.IsNullOrEmpty(proj.Path))
                    BuildProjectOrSolution(GetBranchedPath(proj.Path, branchName));
                else
                    WriteLineIdt($"Path is empty for Project {proj.Name} on { GetBranchedPath(proj.Path, branchName)}");
            }
        }

        static void DbInfo(string dbName = null)
        {
            const string format = @"C:\Windows\Microsoft.NET\{0}\{1}\Config\machine.config";

            var places = new[]{
                new {Framwork = "Framework", ClrVersion= "v2.0.50727"},
                new {Framwork = "Framework", ClrVersion= "v4.0.30319"},
                new {Framwork = "Framework64", ClrVersion= "v2.0.50727"},
                new {Framwork = "Framework64", ClrVersion= "v4.0.30319"},
            };

            foreach (var place in places)
            {
                var xml = new XmlDocument();
                var xmlPath = string.Format(format, place.Framwork, place.ClrVersion);
                xml.Load(xmlPath);
                Console.WriteLine(xmlPath);
                var databases = xml.SelectNodes("//Connections/*");

                foreach (XmlNode db in databases)
                {
                    if (!string.IsNullOrEmpty(dbName) && false == db.Name.Equals(dbName, StringComparison.OrdinalIgnoreCase)) continue;
                    //Console.WriteLine(db.Name);
                    WriteLineIdt("{0,-12}: {1,-10} => {2} : {3}",
                                db.Name,
                                db.SelectSingleNode("add[@key='Database']").Attributes["value"].Value,
                                db.SelectSingleNode("add[@key='Server']").Attributes["value"].Value,
                                db.SelectSingleNode("add[@key='Hashvar']").Attributes["value"].Value);
                }
            }
        }
        #endregion

        static Profile GetProfile()
        {
            try
            {
                var profile = Deserialize<Profile>(ProfileFileName);
                profile?.Projects?.ForEach((p) =>
                {
                    if (p.Path?.Length > 0 && !p.Path.StartsWith(_p4Workspace, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Path = _regP4Workspace.Replace(p.Path, _p4Workspace);
                    }
                    if (p.Entry?.Length > 0 && !p.Entry.StartsWith(_p4Workspace, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Entry = _regP4Workspace.Replace(p.Entry, _p4Workspace);
                    }
                    p.Owner = p.Owner ?? string.Empty;
                    p.Category = p.Category ?? string.Empty;
                    p.Path = p.Path ?? string.Empty;
                    p.Entry = p.Entry ?? string.Empty;
                });
                if (profile != null && profile.Projects != null)
                {
                    var projects = profile.Projects;
                    for (var i = 1; i <= projects.Count; i++) projects[i - 1].Index = i;
                }

                return profile;
            }
            catch (Exception ex)
            {
                WriteLineIdt("error occurred when loading profile file:");
                WriteLineIdt(ex.Message);
                throw;
            }
        }
        static List<Project> GetProjects()
        {
            try
            {
                var projects = Deserialize<List<Project>>();
                projects.ForEach((p) =>
                {
                    if (p.Path?.Length > 0 && !p.Path.StartsWith(_p4Workspace, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Path = _regP4Workspace.Replace(p.Path, _p4Workspace);
                    }
                    if (p.Entry?.Length > 0 && !p.Entry.StartsWith(_p4Workspace, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Entry = _regP4Workspace.Replace(p.Entry, _p4Workspace);
                    }
                    p.Owner = p.Owner ?? string.Empty;
                    p.Category = p.Category ?? string.Empty;
                    p.Path = p.Path ?? string.Empty;
                    p.Entry = p.Entry ?? string.Empty;
                });
                for (var i = 1; i <= projects.Count; i++) projects[i - 1].Index = i;
                return projects;
            }
            catch (Exception ex)
            {
                WriteLineIdt(ex.Message);
                return new List<Project>();
            }
        }

        static void BuildProjectOrSolution(string target2Build, string clrVersion = "4.0")
        {
            string targetDir = string.Format(@".");//this is where mybatch.bat lies
            var proc = new Process();
            proc.StartInfo.WorkingDirectory = targetDir;
            proc.StartInfo.FileName = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\MSBuild.exe";
            proc.StartInfo.Arguments = string.Format("{0} /property:Configuration=Debug", target2Build);//this is argument
            proc.StartInfo.CreateNoWindow = false;
            proc.Start();
        }

        static void SyncCode(string batchFile, string projectName, string branchName, bool force)
        {
            if (string.IsNullOrEmpty(batchFile)) throw new ArgumentNullException("batchFile");
            var targetDir = string.Format(@".");//this is where sync.cmd lies
            var proc = new Process();
            proc.StartInfo.WorkingDirectory = targetDir;
            proc.StartInfo.FileName = batchFile;
            proc.StartInfo.Arguments = $"{projectName} {branchName} {(force ? "-f" : "")}";
            proc.StartInfo.CreateNoWindow = false;
            proc.Start();
        }

        static void InitParametersMappings()
        {
            var keyMappingList = string.Concat(ConfigurationManager.AppSettings["DefaultOptionKeyMappings"]
                , ";", ConfigurationManager.AppSettings["CustomOptionKeyMappings"])
                .Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            keyMappingList.ForEach(row =>
            {
                var kvp = row.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length == 2 && kvp[0].Length > 0 && kvp[1].Length > 0)
                {
                    var keys = kvp[0].ToLowerInvariant().Split(_commaSpaceSeparater, StringSplitOptions.RemoveEmptyEntries);
                    keys.ToList().ForEach(key =>
                    {
                        if (!KeyMapping.ContainsKey(key)) KeyMapping[key] = kvp[1];
                    });
                }
            });


            var valueMappingList = string.Concat(ConfigurationManager.AppSettings["DefaultOptionValueMappings"]
                , ";", ConfigurationManager.AppSettings["CustomOptionValueMappings"])
                .Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            valueMappingList.ForEach(row =>
            {
                var kvp = row.ToLowerInvariant().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length == 2 && kvp[0].Length > 0 && kvp[1].Length > 0)
                {
                    var keys = kvp[0].ToLowerInvariant().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    keys.ToList().ForEach(key =>
                    {
                        if (!ValueMapping.ContainsKey(key)) ValueMapping[key] = kvp[1];
                    });
                }
            });
        }

        static Dictionary<string, string> ResolveOptions(string cmd, string action, string[] args, bool normalizeKeyValues = true)
        {
            action = (action ?? "").ToLower();
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (args == null || args.Length == 0) return dic;
            foreach (var arg in args)
            {
                var m = RegArgs.Match(arg);
                if (m.Success)
                {
                    var key = m.Groups["optkey"].ToString();
                    var value = m.Groups["optval"].ToString();
                    if (key != "") dic[key] = value;
                    key = m.Groups["optflag"].ToString().ToLower();
                    if (key != "") dic[key] = "true";
                }
            }

            if (args.Length == 1 && dic.Count == 0 && cmd != null)
            {
                //set default parameters if RegArgs not matched
                var val = args[0];
                var tmp = string.Concat(",", cmd, ",").ToLower();
                if (",ls,list,".Contains(tmp)) dic["name"] = val;
                if (",sync,bld,build,code,fld,folder,".Contains(tmp)) dic["branch"] = val;
                if (",wiki,url,".Contains(tmp)) dic["type"] = val;
                if (",hosts,".Contains(tmp) && ",open,set,".Contains("," + action + ",")) dic["environment"] = val;
                if (",hosts,".Contains(tmp) && ",find,".Contains("," + action + ",")) dic["host"] = val;
            }

            if (normalizeKeyValues)
            {
                var dic2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in dic)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;
                    if (KeyMapping.ContainsKey(key)) key = KeyMapping[key];
                    if (ValueMapping.ContainsKey(value)) value = ValueMapping[value];

                    dic2.Add(key, value);
                }

                return dic2;
            }

            return dic;
        }

        static bool TryIsMatch(string input, string pattern, RegexOptions option = RegexOptions.IgnoreCase)
        {
            try { return Regex.IsMatch(input, pattern, option); } catch { }
            return false;
        }

        static void WriteLine(string msg = null)
        {
            Console.WriteLine(msg);
        }
        static void WriteLineIf(bool condition, string msg = null)
        {
            if (condition) Console.WriteLine(msg);
        }
        static void WriteIdt(string msg)
        {
            Console.Write(new string(' ', Indent) + msg);
        }

        static void WriteLineIdtIf(bool condition, string msg, params object[] args)
        {
            if (condition) Console.WriteLine(new string(' ', Indent) + msg, args);
        }
        static void WriteLineIdt(string msg, params object[] args)
        {
            Console.WriteLine(new string(' ', Indent) + msg, args);
        }

        static void SetupSampleProfile(IDictionary<string, string> options)
        {
            var profile = new Profile()
            {
                CmdEntries = new List<CmdEntry> {
                    new CmdEntry
                    {
                        Owner="all",
                        Enabled = true,
                        CmdName = "gf",
                        Process = "http://www.tainisoft.com/tools/GoFaster",
                    }
                },
                Projects = new List<Project>
                {
                    new Project
                    {
                        Enabled=true,
                        Owner="alpha",
                        Name ="Slin.MaskEngine",
                        Path = @"c:\p4\REPO1\current\Slin.MaskEngine\Slin.MaskEngine.sln",
                        Wiki = "https://github.com/sw0/Slin.MaskEngine",
                        Endpoints = new List<Endpoint>
                        {
                            new Endpoint{ Type = "Swagger", Url = "https://api.tainisoft.com/api/maskengine/swagger/" }
                        }
                    },
                    new Project
                    {
                        Enabled=true,
                        Owner="team8",
                        Name ="RegexTool",
                        Path = @"c:\p4\REPO2\current\RegexTool\RegexTool.sln",
                        Wiki = "https://github.com/sw0/RegexTool"
                    }
                }
            };

            if (!File.Exists(ProfileFileName) || options.ContainsKey("overwrite"))
            {
                if (File.Exists(ProfileFileName))
                {
                    File.Move(ProfileFileName, ProfileFileName.ToLower().Replace(".xml", $"{DateTime.Now:yyyyMMddHHmmss}.xml"));
                }
                SerializeToXml(profile, ProfileFileName);
                WriteLineIdt("projects.xml got initialized.");
            }

            if (!File.Exists(SyncCmdFileName))
            {
                File.Copy(SyncCmdSampleFileName, SyncCmdFileName, options.ContainsKey("force"));
            }
        }
    }

    public class Profile
    {
        public List<CmdEntry> CmdEntries { get; set; }
        public List<Project> Projects { get; set; }
    }

    public class Project
    {
        [XmlIgnore]
        public int Index { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        /// <summary>
        /// URL: TODO use Endpoint instead
        /// </summary>
        public string Url { get; set; }
        public string Path { get; set; }
        public string Entry { get; set; }
        public string P4SyncBat { get; set; }
        public string Description { get; set; }
        public string Wiki { get; set; }

        public List<Endpoint> Endpoints { get; set; }

        [XmlAttribute] public bool Enabled { get; set; } = true;

        public string ToDescriptionString(string branchName = null, bool detailed = false)
        {
            var solution = branchName == null ? Path : Program.GetBranchedPath(Path, branchName);
            var entry = branchName == null ? Entry : Program.GetBranchedPath(Entry, branchName);
            var endpoints = this.Endpoints?.Where(end => !string.IsNullOrEmpty(end.Url));
            var urls = string.Join("\r\n\t", endpoints.Select(end => end.Url).ToList());

            return $@"{Name}({Category}) | {Owner} | {Url ?? endpoints?.FirstOrDefault()?.Url}
  {solution}
  {entry}
  {urls}
  {P4SyncBat}".TrimEnd();
        }
    }

    public class Endpoint
    {
        [XmlAttribute]
        public string Url { get; set; }
        [XmlAttribute]
        public string Type { get; set; } = "Default";
    }

    public class CmdEntry
    {
        [XmlAttribute]
        public string CmdName { get; set; }
        [XmlAttribute]
        public string Owner { get; set; }
        public string Process { get; set; }
        public string CmdArgs { get; set; }

        public string Description { get; set; }
        [XmlAttribute] public bool Enabled { get; set; } = true;
    }
}