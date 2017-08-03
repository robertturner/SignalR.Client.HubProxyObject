using Castle.DynamicProxy;
using Fasterflect;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public class HubObjectProxy
    {
        static ProxyGenerator proxyGenerator = new ProxyGenerator();
        static Dictionary<Type, MethodInvoker> invokeMethodCache = new Dictionary<Type, MethodInvoker>();

        IHubProxy underlyingHubProxy;

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
            Proxy = proxyGenerator.CreateInterfaceProxyWithTarget(typeof(IHubProxy), new[] { type }, underlyingHubProxy, interceptor);
        }

        public Type Type { get; private set; }
        public object Proxy { get; private set; }

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
                            var del = invokeMethodCache.GetOrSet(retUnderlyingType, () => Parent.underlyingHubProxy.GetType().Method(new Type[] { retUnderlyingType }, "Invoke", new[] { typeof(string), typeof(object[]) }).DelegateForCallMethod());
                            var task = (Task)del(Parent.underlyingHubProxy, invocation.Method.Name, invocation.Arguments);
                            var genTask = task.TryGetAsGenericTask();
                            if (!retIsGenericTask)
                            {
                                task.Wait();
                                invocation.ReturnValue = genTask.Result;
                            }
                            else // Task<T>
                            {
                                var ret = Task_Extensions.CreateGenericTCS(retIsGenericTask ? retUnderlyingType : typeof(bool));
                                invocation.ReturnValue = ret.Task;
                                task.ContinueWith(reply =>
                                {
                                    try
                                    {
                                        if (reply.IsFaulted)
                                            ret.SetException((reply.Exception.InnerExceptions.Count == 1) ? reply.Exception.InnerException : reply.Exception);
                                        else
                                        {
                                            var genRes = reply.TryGetAsGenericTask();
                                            ret.SetResult(genRes.Result);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ret.SetException(ex);
                                    }
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retType != typeof(void))
                        {
                            if (ex is AggregateException agEx && agEx.InnerExceptions.Count == 1)
                                ex = agEx.InnerException;
                            if (retType == typeof(Task))
                                invocation.ReturnValue = ex.AsTaskException();
                            else if (retIsGenericTask)
                                invocation.ReturnValue = ex.AsGenericTaskException(retUnderlyingType);
                            else
                                throw;
                        }
                    }
                }
            }
        }

        public HubConnection Connection { get; private set; }
        public string HubName { get; private set; }


    }
}
