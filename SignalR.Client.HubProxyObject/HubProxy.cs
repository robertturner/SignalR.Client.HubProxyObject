using Castle.DynamicProxy;
using Fasterflect;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public class HubObjectProxy
    {
        static ProxyGenerator proxyGenerator = new ProxyGenerator();
        static Dictionary<Type, MethodInfo> invokeMethodCache = new Dictionary<Type, MethodInfo>();

        IHubProxy underlyingHubProxy;
        Dictionary<string, SignalContainer> signals;

        public HubObjectProxy(HubConnection connection, string hubName, Type type)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrEmpty(hubName))
                throw new ArgumentNullException(nameof(hubName));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!type.IsInterface)
                throw new ArgumentException("type must be interface");
            HubName = hubName;
            Type = type;

            underlyingHubProxy = Connection.CreateHubProxy(HubName);
            var interceptor = new Interceptor(this);
            Proxy = (IHubProxy)proxyGenerator.CreateInterfaceProxyWithTarget(typeof(IHubProxy), new[] { type }, underlyingHubProxy, interceptor);
            signals = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => typeof(HubSignalBase).IsAssignableFrom(p.PropertyType))
                .ToDictionary(p => p.Name, p => new SignalContainer { Property = p });
        }

        public Type Type { get; private set; }
        public IHubProxy Proxy { get; private set; }

        class SignalContainer
        {
            public PropertyInfo Property;
            public HubSignalBase HubSig;
            public IDisposable Disposer;
        }

        class Interceptor : IInterceptor
        {
            HubObjectProxy Parent;
            public Interceptor(HubObjectProxy parent) { Parent = parent; }
            public void Intercept(IInvocation invocation)
            {
                if (invocation.InvocationTarget == Parent.underlyingHubProxy)
                    invocation.Proceed();
                else
                {
                    if (invocation.Method.IsSpecialName)
                    {
                        bool handled = false;
                        throw new NotImplementedException();
                        var methodName = invocation.Method.Name;
                        if (methodName.StartsWith("get_"))
                        {
#if false
                            var name = methodName.Substring(4);
                            if (Parent.signals.TryGetValue(name, out SignalContainer sig))
                            {
                                handled = true;
                                if (sig.HubSig == null)
                                {
                                    MethodInfo subscriber;
                                    if (sig.Property.PropertyType.IsGenericType)
                                    {
                                        var argType = sig.Property.PropertyType.GetGenericArguments()[0];

                                        var callbackType = typeof(Action<>).MakeGenericType(argType);
                                        subscriber = typeof(HubProxyExtensions).Method(new[] { argType }, "On", new Type[] { typeof(IHubProxy), typeof(string), callbackType }, Flags.StaticPublic);

                                        var genSig = typeof(HubSignal<>).MakeGenericType(argType);
                                        var ctor = genSig.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
                                        sig.HubSig = (HubSignalBase)ctor[0].Invoke(new object[] { null, name });
                                    }
                                    else
                                    {
                                        subscriber = typeof(HubProxyExtensions).Method("On", new Type[] { typeof(IHubProxy), typeof(string), typeof(Action) }, Flags.Static);
                                        sig.HubSig = new HubSignal(null, name);
                                    }
                                    sig.Disposer = (IDisposable)subscriber.Invoke(null, new object[] { Parent.Proxy, name, sig.HubSig.GetCaller() });
                                }
                                invocation.ReturnValue = sig.HubSig;
                            }
#endif
                        }
                        if (!handled)
                            throw new NotImplementedException($"Method: {methodName}");
                    }
                    else
                    {
                        var retType = invocation.Method.ReturnType;
                        Type retUnderlyingType = retType;
                        var retIsGenericTask = retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>);
                        if (retIsGenericTask)
                            retUnderlyingType = retType.GetGenericArguments()[0];
                        var retIsTask = retType == typeof(Task) || retIsGenericTask;
                        try
                        {
                            if (retType == typeof(void) || retType == typeof(Task))
                            {
                                var task = Parent.underlyingHubProxy.Invoke(invocation.Method.Name, invocation.Arguments);
                                if (retType == typeof(void))
                                {
                                    invocation.ReturnValue = null;
                                    task.Wait();
                                }
                                else
                                    invocation.ReturnValue = task;
                            }
                            else // have results
                            {
                                var method = invokeMethodCache.GetOrSet(retUnderlyingType, () => Parent.underlyingHubProxy.GetType().Method(new Type[] { retUnderlyingType }, "Invoke", new[] { typeof(string), typeof(object[]) }));
                                var task = (Task)method.Invoke(Parent.underlyingHubProxy, new object[] { invocation.Method.Name, invocation.Arguments });
                                if (!retIsGenericTask)
                                {
                                    var genTask = task.TryGetAsGenericTask();
                                    task.Wait();
                                    invocation.ReturnValue = genTask.Result;
                                }
                                else // Task<T>
                                    invocation.ReturnValue = task;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (retType != typeof(void))
                            {
                                if (ex is AggregateException agEx && agEx.InnerExceptions.Count == 1)
                                    ex = agEx.InnerException;
                                if (retType == typeof(Task))
                                    invocation.ReturnValue = Task.FromException(ex);
                                else if (retIsGenericTask)
                                    invocation.ReturnValue = ex.AsGenericTaskException(retUnderlyingType);
                                else
                                    throw;
                            }
                        }
                    }
                }
            }
        }

        public HubConnection Connection { get; private set; }
        public string HubName { get; private set; }


    }
}
