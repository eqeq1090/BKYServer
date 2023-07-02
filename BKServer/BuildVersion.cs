using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Config
{
    public static class BuildVersion
    {
        public static readonly bool IsEnabled = false;
        public static readonly string GitCommitHash = "$GitCommitHash$";
        public static readonly string BuildTime = "$BuildTime$";
        public static readonly string BuildNumber = "$BuildNumber$";
        public static readonly string GitBranchName = "$GitBranchName$";
        public static readonly string ProtocolVersion = BKProtocol.ProtocolVersion.Value;
        public static readonly string TableVersion = "1.0.0";
        public static readonly string QuantumVersion = "1.0.0";
    }
}
