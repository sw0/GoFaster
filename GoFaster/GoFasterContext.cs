using GoFaster.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GoFaster
{
    public class GoFasterContext
    {
        public Profile Profile { get; set; }

        public GoFasterContext()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                var path = Path.Combine(Environment.CurrentDirectory + "/profile.xml");
                var profile = Deserialize<Profile>(path);

                NormalizeProfile(profile);

                Profile = profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine("error occurred when load: {0}", ex.Message);
            }
        }

        private static void NormalizeProfile(Profile profile)
        {
            profile.Projects = profile.Projects ?? new List<Project>();

            var i = 1;
            foreach (var p in profile.Projects)
            {
                p.Index = i;
                i++;
            }

            foreach (var p in profile.Projects)
            {
                p.Owners ??= new List<string>();
                p.Categories ??= new List<string>();

                p.Owners = p.Owners.Where(o => !string.IsNullOrEmpty(o))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                p.Categories = p.Categories.Where(o => !string.IsNullOrEmpty(o))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        static void SerializeToXml<T>(T obj, string path = @".\proflie.xml") where T : class
        {
            if (obj == null) return;
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                var sb = new StringBuilder();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

                xmlSerializer.Serialize(sw, obj);
            }
        }

        static T Deserialize<T>(string path = @".\proflie.xml") where T : class
        {
            using (Stream strm = File.OpenRead(path))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                var result = xmlSerializer.Deserialize(strm) as T;
                return result;
            }
        }
    }
}
