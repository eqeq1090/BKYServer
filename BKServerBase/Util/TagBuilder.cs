using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Util
{
    public static class TagBuilder
    {
        public static string MakeTag(string file, int line)
        {
            return string.Intern($"{Path.GetFileName(file)}:{line.ToString()}");
        }
    }
}
