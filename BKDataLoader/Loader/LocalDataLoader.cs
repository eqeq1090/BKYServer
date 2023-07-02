using System.IO;
using System.Linq;

namespace BKDataLoader.Loader
{
    public class LocalDataLoader : IDataLoader
    {
        private string m_LocalPath;
        public LocalDataLoader(string localPath)
        {
            m_LocalPath = localPath;
        }
        public override string[] GetFiles(string path)
        {
            return Directory.GetFiles(Path.Combine(m_LocalPath, path)).Select((filename) =>
            {
                if (filename.StartsWith(m_LocalPath))
                {
                    filename = filename.Substring(m_LocalPath.Length);
                }
                if (filename.StartsWith("\\") | filename.StartsWith("/"))
                {
                    filename = filename.Substring(1);
                }
                return filename;
            }).ToArray();
        }

        public override string GetDiskPath(string path)
        {
            return Path.Combine(m_LocalPath, path);
        }
    }
}
