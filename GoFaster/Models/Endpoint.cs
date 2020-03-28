using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GoFaster.Models
{
    public class Endpoint
    {
        [XmlAttribute]
        public string Url { get; set; }
        [XmlAttribute]
        public string Type { get; set; } = "Default";
    }
}
