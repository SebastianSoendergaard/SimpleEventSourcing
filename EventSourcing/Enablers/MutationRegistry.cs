using System.Collections.Concurrent;

namespace EventSourcing.Enablers;

public class MutationRegistry
{
    private static readonly ConcurrentDictionary<Type, HashSet<Type>> _registry = new();

    public static bool CanMutate(object handler, object argument, Type intefaceType)
    {
        var argumentType = argument?.GetType() ?? throw new ArgumentNullException("Argument can not be null");
        var availableMethodTypes = GetAvailableMethodTypes(handler, intefaceType);
        return availableMethodTypes.Contains(argumentType);
    }

    public static ISet<Type> GetAvailableMethodTypes(object handler, Type intefaceType)
    {
        var handlerType = handler?.GetType() ?? throw new ArgumentNullException("Handler can not be null");

        if (!_registry.TryGetValue(handlerType, out var argumentTypes))
        {
            argumentTypes = [];

            var interfaces = handlerType
                .GetInterfaces()
                .Where(x => x.IsGenericType)
                .Where(x => x.GetGenericTypeDefinition() == intefaceType);

            foreach (var iface in interfaces)
            {
                var eventType = iface.GetGenericArguments()[0];
                argumentTypes.Add(eventType);
            }

            _registry.TryAdd(handlerType, argumentTypes);
        }

        return argumentTypes;
    }
}
