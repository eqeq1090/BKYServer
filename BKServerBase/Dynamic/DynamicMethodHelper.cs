using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Dynamic
{
    public static class DynamicMethodHelper
    {
        public delegate object ActivateObject();
        public delegate object ActivateObjectWithArg<T>(T arg);

        public static ActivateObject CreateActivateObject(Type type)
        {
            var method = new DynamicMethod("ActivateObject", type, null, true);
            var generator = method.GetILGenerator();

            var ctor = type.GetConstructor(Type.EmptyTypes);
            generator.Emit(OpCodes.Newobj, ctor!);
            generator.Emit(OpCodes.Ret);
            var emitActivate = (ActivateObject)method.CreateDelegate(typeof(ActivateObject));
            return emitActivate;
        }

        public static ActivateObjectWithArg<T1> CreateActivateObject<T1>(Type type)
        {
            var paramTypes = new Type[] { typeof(T1) };

            var method = new DynamicMethod(
                name: "ActivateObjectWithArg", 
                returnType: type, 
                parameterTypes: paramTypes);
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);

            var ctor = type.GetConstructor(paramTypes);
            generator.Emit(OpCodes.Newobj, ctor!);
            
            generator.Emit(OpCodes.Ret);
            var emitActivate = (ActivateObjectWithArg<T1>)method.CreateDelegate(typeof(ActivateObjectWithArg<T1>));
            return emitActivate;
        }
    }
}
