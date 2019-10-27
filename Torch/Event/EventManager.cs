using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using Torch.API;
using Torch.API.Event;
using Torch.Managers;

namespace Torch.Event
{
    /// <summary>
    /// Manager class responsible for managing registration and dispatching of events.
    /// </summary>
    public class EventManager : Manager, IEventManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<Type, IEventList> EventLists = new Dictionary<Type, IEventList>();

        internal static void AddDispatchShims(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
                if (type.HasAttribute<EventShimAttribute>())
                    AddDispatchShim(type);
        }

        private static readonly HashSet<Type> DispatchShims = new HashSet<Type>();
        private static void AddDispatchShim(Type type)
        {
            lock (DispatchShims)
                if (!DispatchShims.Add(type))
                    return;
            if (!type.IsSealed || !type.IsAbstract)
                Log.Warn($"Registering type {type.FullName} as an event dispatch type, even though it isn't declared singleton");
            var listsFound = 0;
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(EventList<>))
                {
                    Type eventType = field.FieldType.GenericTypeArguments[0];
                    if (EventLists.ContainsKey(eventType))
                        Log.Error($"Ignore event dispatch list {type.FullName}#{field.Name}; we already have one.");
                    else
                    {
                        EventLists.Add(eventType, (IEventList)field.GetValue(null));
                        listsFound++;
                    }

                }
            if (listsFound == 0)
                Log.Warn($"Registering type {type.FullName} as an event dispatch type, even though it has no event lists.");
        }


        /// <summary>
        /// Gets all event handler methods declared by the given type and its base types.
        /// </summary>
        /// <param name="exploreType">Type to explore</param>
        /// <returns>All event handler methods</returns>
        private static IEnumerable<MethodInfo> EventHandlers(Type exploreType)
        {
            IEnumerable<MethodInfo> enumerable = exploreType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(x =>
                {
                    var attr = x.GetCustomAttribute<EventHandlerAttribute>();
                    if (attr == null)
                        return false;
                    ParameterInfo[] ps = x.GetParameters();
                    if (ps.Length != 1)
                        return false;
                    return ps[0].ParameterType.IsByRef && typeof(IEvent).IsAssignableFrom(ps[0].ParameterType.GetElementType());
                });
            return exploreType.BaseType != null ? enumerable.Concat(EventHandlers(exploreType.BaseType)) : enumerable;
        }
        
        private static void RegisterHandlerInternal(IEventHandler instance)
        {
            var foundHandler = false;
            foreach (MethodInfo handler in EventHandlers(instance.GetType()))
            {
                Type eventType = handler.GetParameters()[0].ParameterType.GetElementType();
                Debug.Assert(eventType != null);
                foundHandler = true;
                if (eventType.IsInterface)
                {
                    var foundList = false;
                    foreach (KeyValuePair<Type, IEventList> kv in EventLists)
                        if (eventType.IsAssignableFrom(kv.Key))
                        {
                            kv.Value.AddHandler(handler, instance);
                            foundList = true;
                        }
                    if (foundList)
                        continue;
                }
                else if (EventLists.TryGetValue(eventType, out IEventList list))
                {
                    list.AddHandler(handler, instance);
                    continue;
                }
                Log.Error($"Unable to find event handler list for event type {eventType.FullName}");
            }
            if (!foundHandler)
                Log.Warn($"Found no handlers in {instance.GetType().FullName} or base types");

        }

        /// <summary>
        /// Unregisters all handlers owned by the given instance
        /// </summary>
        /// <param name="instance">Instance</param>
        private static void UnregisterHandlerInternal(IEventHandler instance)
        {
            foreach (IEventList list in EventLists.Values)
                list.RemoveHandlers(instance);
        }

        private Dictionary<Assembly, HashSet<IEventHandler>> _registeredHandlers = new Dictionary<Assembly, HashSet<IEventHandler>>();

        /// <inheritdoc/>
        public EventManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        /// <summary>
        /// Registers all event handler methods contained in the given instance 
        /// </summary>
        /// <param name="handler">Instance to register</param>
        /// <returns><b>true</b> if added, <b>false</b> otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool RegisterHandler(IEventHandler handler)
        {
            Assembly caller = Assembly.GetCallingAssembly();
            lock (_registeredHandlers)
            {
                if (!_registeredHandlers.TryGetValue(caller, out HashSet<IEventHandler> handlers))
                    _registeredHandlers.Add(caller, handlers = new HashSet<IEventHandler>());
                if (handlers.Add(handler))
                {
                    RegisterHandlerInternal(handler);
                    return true;
                }
                return false;
            }
        }


        /// <summary>
        /// Unregisters all event handler methods contained in the given instance 
        /// </summary>
        /// <param name="handler">Instance to unregister</param>
        /// <returns><b>true</b> if removed, <b>false</b> otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool UnregisterHandler(IEventHandler handler)
        {
            Assembly caller = Assembly.GetCallingAssembly();
            lock (_registeredHandlers)
            {
                if (!_registeredHandlers.TryGetValue(caller, out HashSet<IEventHandler> handlers))
                    return false;
                if (handlers.Remove(handler))
                {
                    UnregisterHandlerInternal(handler);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Unregisters all handlers owned by the given assembly.
        /// </summary>
        /// <param name="asm">Assembly to unregister</param>
        /// <param name="callback">Optional callback invoked before a handler is unregistered.  Ignored if null</param>
        /// <returns>the number of handlers that were unregistered</returns>
        internal int UnregisterAllHandlers(Assembly asm, Action<IEventHandler> callback = null)
        {
            lock (_registeredHandlers)
            {
                if (!_registeredHandlers.TryGetValue(asm, out HashSet<IEventHandler> handlers))
                    return 0;
                foreach (IEventHandler k in handlers)
                {
                    callback?.Invoke(k);
                    UnregisterHandlerInternal(k);
                }
                int count = handlers.Count;
                handlers.Clear();
                _registeredHandlers.Remove(asm);
                return count;
            }
        }
    }
}
