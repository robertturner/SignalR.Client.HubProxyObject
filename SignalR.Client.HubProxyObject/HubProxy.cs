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
        Dictionary<string, SignalContainer> events; // null if obj disposed

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
            Proxy = (IHubProxy)proxyGenerator.CreateInterfaceProxyWithTarget(typeof(IHubProxy), new[] { type, typeof(IDisposable) }, underlyingHubProxy, interceptor);
            events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, p => new SignalContainer { EventInfo = p });
        }

        public Type Type { get; private set; }
        public IHubProxy Proxy { get; private set; }

        class SignalContainer
        {
            public Delegate Callers;
            public EventInfo EventInfo;
            public IDisposable Disposer;
        }

        class Interceptor : IInterceptor
        {
            HubObjectProxy Parent;
            public Interceptor(HubObjectProxy parent) { Parent = parent; }
            public void Intercept(IInvocation invocation)
            {
                if (Parent.events == null)
                    throw new ObjectDisposedException(Parent.Type.Name);
                if (invocation.InvocationTarget == Parent.underlyingHubProxy)
                    invocation.Proceed();
                else
                {
                    if (invocation.Method.IsSpecialName)
                    {
                        var methods = Parent.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                            .Where(m => m.IsSpecialName).ToArray();
                        bool handled = false;
                        var methodName = invocation.Method.Name;
                        if (methodName.StartsWith("add_"))
                        {
                            var name = methodName.Substring(4);
                            if (invocation.Arguments.Length != 1 || !(invocation.Arguments[0] is Delegate @delegate))
                                throw new ArgumentException("Expect event adding argument to be single argument of delegate type");
                            if (Parent.events.TryGetValue(name, out SignalContainer sigCont))
                            {
                                var doSub = sigCont.Callers == null;
                                sigCont.Callers = Delegate.Combine(sigCont.Callers, @delegate);
                                if (doSub)
                                {
                                    // Subscribe
                                    MethodInfo subscriber;
                                    if (sigCont.EventInfo.EventHandlerType.IsGenericType)
                                    {
                                        //var argType = sigCont.EventInfo.EventHandlerType.GetGenericArguments()[0];

                                        //var callbackType = typeof(Action<>).MakeGenericType(argType);
                                        //subscriber = typeof(HubProxyExtensions).Method(new[] { argType }, "On", new Type[] { typeof(IHubProxy), typeof(string), sigCont.EventInfo.EventHandlerType }, Flags.StaticPublic);
                                        subscriber = typeof(HubProxyExtensions).Method(sigCont.EventInfo.EventHandlerType.GetGenericArguments(), "On", new Type[] { typeof(IHubProxy), typeof(string), sigCont.EventInfo.EventHandlerType }, Flags.StaticPublic);

                                        //var genSig = typeof(HubSignal<>).MakeGenericType(argType);
                                        //var ctor = genSig.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
                                        //sig.HubSig = (HubSignalBase)ctor[0].Invoke(new object[] { null, name });
                                    }
                                    else
                                    {
                                        subscriber = typeof(HubProxyExtensions).Method("On", new Type[] { typeof(IHubProxy), typeof(string), typeof(Action) }, Flags.Static);
                                        //sig.HubSig = new HubSignal(null, name);
                                    }
                                    sigCont.Disposer = (IDisposable)subscriber.Invoke(null, new object[] { Parent.Proxy, name, sigCont.Callers });

                                }
                                handled = true;
                            }
                        }
                        else if (methodName.StartsWith("remove_"))
                        {
                            var name = methodName.Substring(7);
                            if (invocation.Arguments.Length != 1 || !(invocation.Arguments[0] is Delegate @delegate))
                                throw new ArgumentException("Expect event adding argument to be single argument of delegate type");
                            if (Parent.events.TryGetValue(name, out SignalContainer sigCont))
                            {
                                sigCont.Callers = Delegate.Remove(sigCont.Callers, @delegate);
                                if (sigCont.Callers  == null)
                                {
                                    // Delete subscription
                                }
                                handled = true;
                            }
                        }
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
                        }
#endif
                        if (!handled)
                            throw new NotImplementedException($"Method: {methodName}");
                    }
                    else
                    {
                        if (invocation.Method.Name == "Dispose")
                        {
                            foreach (var sig in Parent.events)
                            {
                                sig.Value.Disposer?.Dispose();
                                sig.Value.Callers = null;
                            }
                            Parent.events = null;
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
        }

        public HubConnection Connection { get; private set; }
        public string HubName { get; private set; }
    }
}
