using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace BKServerBase.Util
{
    public class CommonUtil
    {
        public static string TruncateJsonString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            if (text.Length <= maxLength)
            {
                return text;
            }
            char[] delimiters = new char[] { '}', ']' };
            int index = text.LastIndexOfAny(delimiters, maxLength - 3);
            if (index > maxLength / 2)
            {
                return text.Substring(0, index) + "...";
            }
            else
            {
                return text.Substring(0, maxLength - 3) + "...";
            }
        }

        public static void GetNics(ref List<string> IPv4List, ref List<string> IPv6List)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (NetworkInterfaceType.Loopback == adapter.NetworkInterfaceType)
                {
                    continue;
                }

                if (adapter.Description.ToLower().Contains("docker") |
                adapter.Description.ToLower().Contains("vmware") |
                adapter.Description.ToLower().Contains("virtual") ||
                adapter.Description.ToLower().Contains("parallels") |
                adapter.Description.ToLower().Contains("pseudo"))
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation ipInfo in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (System.Net.Sockets.AddressFamily.InterNetwork == ipInfo.Address.AddressFamily)
                    {
                        IPv4List.Add(ipInfo.Address.ToString());
                    }
                    else if (System.Net.Sockets.AddressFamily.InterNetworkV6 == ipInfo.Address.AddressFamily)
                    {
                        IPv6List.Add(ipInfo.Address.ToString());
                    }
                }
            }
        }

        public static string CreateDirectoryMd5(string srcPath)
        {
            var filePaths = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

            using (var md5 = MD5.Create())
            {
                foreach (var filePath in filePaths)
                {
                    // hash path
                    byte[] pathBytes = Encoding.UTF8.GetBytes(filePath);
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    // hash contents
                    byte[] contentBytes = File.ReadAllBytes(filePath);

                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                //Handles empty filePaths case
                md5.TransformFinalBlock(new byte[0], 0, 0);

                if (md5.Hash == null)
                {
                    return string.Empty;
                }
                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
        }

        public static void GetNicsV4(out List<string> ipV4List)
        {
            ipV4List = new List<string>();

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (NetworkInterfaceType.Loopback == adapter.NetworkInterfaceType)
                {
                    continue;
                }

                if (adapter.Description.ToLower().Contains("docker") |
                adapter.Description.ToLower().Contains("vmware") |
                adapter.Description.ToLower().Contains("virtual") ||
                adapter.Description.ToLower().Contains("parallels") |
                adapter.Description.ToLower().Contains("pseudo"))
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation ipInfo in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (System.Net.Sockets.AddressFamily.InterNetwork == ipInfo.Address.AddressFamily)
                    {
                        ipV4List.Add(ipInfo.Address.ToString());
                    }
                }
            }
        }

        public static string GetFirstNicsV4()
        {
            GetNicsV4(out var ipv4List);

            return ipv4List.First();
        }
    }
}
