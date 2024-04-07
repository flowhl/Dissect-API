using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace DissectAPI
{
    public static class Globals
    {
        // Serialize an object to XML and save it to a file
        public static void SerializeToFile<T>(this T obj, string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, obj);
            }
        }

        // Deserialize an object from an XML file
        public static T DeserializeFromFile<T>(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T)); 
            using (TextReader reader = new StreamReader(path))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static string XStratInstallPath = GetXStratInstallPath();
        public static string DissectPath = GetDissectPath(XStratInstallPath);

        public static string GetDissectPath(string XStratInstallPath)
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                string dissectPath = Path.Combine(XStratInstallPath, "External", "Windows", "r6-dissect.exe");
                if (File.Exists(dissectPath)) return dissectPath;
            }
            else
            {
                string dissectPath = Path.Combine(XStratInstallPath, "External", "Linux", "r6-dissect");
                if (File.Exists(dissectPath)) return dissectPath;
            }
            return null;
        }

        
        public static string GetXStratInstallPath()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows) return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            else
                return "/app";
        }
    }
}
