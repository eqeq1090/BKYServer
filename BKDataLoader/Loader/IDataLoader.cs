using System.IO;

namespace BKDataLoader.Loader
{
    public abstract class IDataLoader
    {
        public Stream GetStream(string path)
        {
            return new FileStream(GetDiskPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        public abstract string[] GetFiles(string path);
        public abstract string GetDiskPath(string path);
    }
}
