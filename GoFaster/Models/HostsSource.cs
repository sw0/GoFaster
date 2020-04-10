using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GoFaster.Models
{
    public class HostsFile
    {
        [XmlAttribute]
        public string Environment { get; set; }

        [XmlText]
        public string FileName { get; set; }
    }

    public class HostsSource : List<HostsFile>
    {
        [XmlAttribute]
        public string Name { get; set; }

        //[XmlArrayItem("HostsFile")]
        //public List<HostsFile> HostsFiles { get; set; }

    }
}
