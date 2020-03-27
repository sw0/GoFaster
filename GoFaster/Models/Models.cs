using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GoFaster.Models
{
    public class Profile
    {
        public List<CmdEntry> CmdEntries { get; set; }
        public List<Project> Projects { get; set; }
    }

    public class Project
    {
        private static char[] Splitter = new[] { ' ', ';', ',' };
        //public string[] GetOwners()
        //{
        //    return Owner?.Split(Splitter, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        //}

        [XmlIgnore]
        public int Index { get; set; }

        public List<string> _categories { get; set; }
        [XmlArrayItem("Name")]
        public List<string> Categories
        {
            get { return _categories; }
            set
            {
                _categories = value ?? new List<string>();
                _categories = _categories.Where(r => !string.IsNullOrEmpty(r)).ToList();
            }
        }

        public string Name { get; set; }

        public List<string> _owners { get; set; }
        [XmlArrayItem("Owner")]
        public List<string> Owners
        {
            get { return _owners; }
            set
            {
                _owners = value ?? new List<string>();
                _owners = _owners.Where(r => !string.IsNullOrEmpty(r)).ToList();
            }
        }
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

        public Project()
        {
            Owners = new List<string>();
            Categories = new List<string>();
        }

        internal string GetTitle()
        {
            return $"#{Index.ToString("d3")} {Name}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="branchName"></param>
        /// <returns></returns>
        /// <remarks>
        /// **Example:**
        /// PROJECT_NAME | Owner1, Owner2 | Category1,Category2 | FIRST_ENDPOINT
        /// CODE PATH: 
        /// Endpoints:
        ///   Default   https://github.com/0sw/GoFaster
        ///   Swagger   https://api.tainisoft.com/swagger
        /// </remarks>
        internal string GetDetailedDescription(string branchName = null)
        {
            var sb = new StringBuilder();
            #region first line of title: <Name> | <Owners> | <Categories> | <FRIST_Endpoint>

            sb.Append($"#{Index.ToString("d3")} {Name}");

            if (Owners.Any())
            {
                sb.Append(" | ");
                sb.Append(string.Join(",", Owners));
            }

            if (Categories.Any())
            {
                sb.Append(" | ");
                sb.Append(string.Join(",", Categories));
            }

            var ends = Endpoints.Where(e => !string.IsNullOrEmpty(e.Url)).ToList();

            if (ends.Any())
            {
                sb.Append($"| {ends.First().Url}");
            }

            sb.AppendLine();
            #endregion

            if (!string.IsNullOrEmpty(Path))
            {
                sb.AppendLine($"Code: {Path}");
            }
            if (!string.IsNullOrEmpty(Entry))
            {
                sb.AppendLine($"ENTRY: {Entry}");
            }

            #region Endpoints
            if (ends.Any())
            {
                sb.AppendLine($"Endpoints:");
                ends.ForEach(e =>
                {
                    sb.AppendLine($"  {e.Type}\t{e.Url}");
                });
            } 
            #endregion

            return sb.ToString();
        }
    }

    public class Endpoint
    {
        [XmlAttribute]
        public string Url { get; set; }
        [XmlAttribute]
        public string Type { get; set; } = "Default";
    }


    public enum CmdTargetType
    {
        Command, //TODO does powershell supported?
        ApplicationOrUrl,
    }

    public class CmdEntry
    {
        [XmlAttribute]
        public string CmdName { get; set; }
        [XmlAttribute]
        public string ShortCmdNames { get; set; }
        public CmdTargetType TargetType { get; set; }
        [XmlAttribute]
        public string Owner { get; set; }
        public string Process { get; set; }
        public string CmdArgs { get; set; }

        public string Description { get; set; }
        [XmlAttribute] public bool Enabled { get; set; } = true;
    }
}
