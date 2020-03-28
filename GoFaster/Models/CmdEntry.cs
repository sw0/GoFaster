using System;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GoFaster.Models
{
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
