using Amazon;
using Amazon.S3;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BKServerBase.Config;

namespace BKDataLoader.Loader
{
    public class MinioDataLoader : IDataLoader
    {
        private AmazonS3Client m_minioClient;
        private string m_minioBucket;
        private string m_localCacheDir;

        public MinioDataLoader(string url, string accessKey, string secretKey, string bucketName)
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1, // MUST set this before setting ServiceURL and it should match the MINIO_REGION enviroment variable.
                ServiceURL = "http://" + url,
                ForcePathStyle = true // MUST be true to work correctly with MinIO server
            };
            m_minioClient = new AmazonS3Client(accessKey, secretKey, config);
            m_minioBucket = bucketName;
            m_localCacheDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location!)!, 
                $"{ConfigManager.Instance.ServerMode}","cache");
            if (Directory.Exists(m_localCacheDir) == false)
            {
                Directory.CreateDirectory(m_localCacheDir);
            }
        }

        private T WaitTask<T>(Task<T> task)
        {
            try
            {
                task.Wait();
                return task.Result;
            }

            catch (AggregateException e)
            {
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }
                throw;
            }
        }

        public override string[] GetFiles(string path)
        {
            var result = WaitTask(m_minioClient.ListObjectsAsync(m_minioBucket, path));
            return result.S3Objects.Select((x) => x.Key).ToArray();
        }

        private static string NormalizeETag(string etag)
        {
            return etag.Replace("\"", "");
        }

        public override string GetDiskPath(string path)
        {
            try
            {
                var metadata = WaitTask(m_minioClient.GetObjectMetadataAsync(m_minioBucket, path));
                var cacheFilename = Path.Combine(m_localCacheDir, NormalizeETag(metadata.ETag));
                if (File.Exists(cacheFilename))
                {
                    return cacheFilename;
                }
                using (var response = WaitTask(m_minioClient.GetObjectAsync(m_minioBucket, path)))
                {
                    cacheFilename = Path.Combine(m_localCacheDir, NormalizeETag(response.ETag));
                    var tmpCacheFilename = Path.Combine(m_localCacheDir, NormalizeETag(response.ETag) + ".tmp");
                    using (var diskStream = new FileStream(tmpCacheFilename, FileMode.Create))
                    {
                        response.ResponseStream.CopyTo(diskStream);
                    }
                    File.Move(tmpCacheFilename, cacheFilename);
                }
                return cacheFilename;
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("Resource Not found", path);
                }
                throw;
            }
        }
    }
}
