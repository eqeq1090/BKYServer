using Swan;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Dynamic;
using BKGameServerComponent.Actor;
using static BKServerBase.Dynamic.DynamicMethodHelper;

namespace BKGameServerComponent.Controller.Detail
{
    internal interface IControllerCollection
    {
        T GetController<T>()
            where T : class, IPlayerController;
    }

    internal sealed class ControllerCollection : IControllerCollection
    {
        private static readonly HashSet<(Type, ActivateObjectWithArg<Player>)> ControllerTypeSet = new HashSet<(Type, ActivateObjectWithArg<Player>)>();
        private readonly Dictionary<Type, IPlayerController> m_ControllerMap = new Dictionary<Type, IPlayerController>();

        static ControllerCollection()
        {
            var assembly = Assembly.GetExecutingAssembly();
            if (assembly is null)
            {
                throw new Exception($"assembly is null");
            }

            var types = assembly.GetAllTypes();
            foreach (var type in types)
            {
                if (type.IsClass is false)
                {
                    continue;
                }

                if (type.IsAssignableTo(typeof(IPlayerController)) is false)
                {
                    continue;
                }

                var activateObjectMethod = CreateActivateObject<Player>(type);
                ControllerTypeSet.Add((type, activateObjectMethod));
            }
        }

        public ControllerCollection(Player player)
        {
            foreach (var (controllerType, activateObject) in ControllerTypeSet)
            {
                var controllerInstance = activateObject.Invoke(player) as IPlayerController;
                if (controllerInstance is null)
                {
                    throw new Exception($"controllerInstance is null");
                }

                m_ControllerMap.Add(controllerType, controllerInstance);
            }
        }

        public T GetController<T>()
            where T : class, IPlayerController
        {
            if (m_ControllerMap.TryGetValue(typeof(T), out var controller) is false)
            {
                throw new Exception($"Controller is not registered, type: {nameof(T)}");
            }

            return (T)controller;
        }
    }
}
