using System.Collections.Generic;

namespace GoFaster.Models
{
    public class Profile
    {
        public List<CmdEntry> CmdEntries { get; set; }

        public List<HostsSource> HostsSources { get; set; }

        public List<Project> Projects { get; set; }
    }
}
