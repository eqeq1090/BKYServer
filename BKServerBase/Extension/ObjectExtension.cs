using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BKServerBase.Extension
{
    public static class ObjectExtension
    {
        public static T DeepCopy<T>(this object obj)
            where T : class
        {
            var bf = new BinaryFormatter();
            
#pragma warning disable SYSLIB0011 // 형식 또는 멤버는 사용되지 않습니다.

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);

                return (T)bf.Deserialize(ms);
            }

#pragma warning restore SYSLIB0011 // 형식 또는 멤버는 사용되지 않습니다.
        }

        public static object DeepCopy(this object obj)
        {
            var bf = new BinaryFormatter();

#pragma warning disable SYSLIB0011 // 형식 또는 멤버는 사용되지 않습니다.

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);

                return bf.Deserialize(ms);
            }

#pragma warning restore SYSLIB0011 // 형식 또는 멤버는 사용되지 않습니다.
        }
    }
}
