using Fasterflect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject
{
    public static class Task_Extensions
    {

        public static readonly Task EmptyTask = Task.FromResult<bool>(true);

        public static Task<T> GetEmptyTask<T>() { return Task.FromResult<T>(default(T)); }

        public static Task<T> AsTask<T>(this T obj)
        {
            return Task.FromResult<T>(obj);
        }

        public static Task<T> AsTask<T>(this T obj, object state)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(state);
            tcs.SetResult(obj);
            return tcs.Task;
        }

        public static Task AsTaskException(this Exception exception)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        #region Generic TaskCompletionSource
        public static TaskCompletionSourceGeneric CreateGenericTCS(Type type)
        {
            return TaskCompletionSourceGeneric.Create(type);
        }

        public static Task TCSGenericFromResult(Type type, object result)
        {
            var tcs = CreateGenericTCS(type);
            tcs.SetResult(result);
            return tcs.Task;
        }
        public static Task AsGenericTaskResult(this object result, Type type)
        {
            return TCSGenericFromResult(type, result);
        }


        public static Task TCSGenericFromException(Type type, Exception exception)
        {
            var tcs = CreateGenericTCS(type);
            tcs.SetException(exception);
            return tcs.Task;
        }
        public static Task AsGenericTaskException(this Exception exception, Type type)
        {
            return TCSGenericFromException(type, exception);
        }

        public static TaskCompletionSourceGeneric AsTCSGeneric(this Type type)
        {
            return CreateGenericTCS(type);
        }

        public class TaskCompletionSourceGeneric
        {
            private static readonly Dictionary<Type, TaskDelegateCacheContainer> tdcCache = new Dictionary<Type, TaskDelegateCacheContainer>();

            private TaskDelegateCacheContainer cont;
            public Type Type { get { return cont.Type; } }
            public Type TCSType { get { return cont.TCSType; } }
            private object instance;

            public object Instance
            {
                get
                {
                    if (instance == null)
                        instance = cont.CreateTcs();
                    return instance;
                }
            }

            private TaskCompletionSourceGeneric(Type type, TaskDelegateCacheContainer cont)
            {
                this.cont = cont;
            }

            public static TaskCompletionSourceGeneric Create(Type type)
            {
                if (Object.ReferenceEquals(null, type))
                    throw new ArgumentNullException(nameof(type));
                TaskDelegateCacheContainer cont;
                if (!tdcCache.TryGetValue(type, out cont))
                {
                    cont = new TaskDelegateCacheContainer(type);
                    tdcCache.Add(type, cont);
                }
                return new TaskCompletionSourceGeneric(type, cont);
            }

            public void SetResult(object result) { cont.SetResCaller(Instance, result); }

            public void SetException(Exception exception)
            {
                if (Object.ReferenceEquals(null, exception))
                    throw new ArgumentNullException(nameof(exception));
                cont.SetExceptionCaller(Instance, exception);
            }

            public Task Task { get { return (Task)cont.GetTaskCaller(Instance); } }

            private class TaskDelegateCacheContainer
            {
                public Type Type { get; private set; }
                public Type TCSType { get; private set; }

                private MethodInvoker setResCaller, getTaskCaller, setExceptionCaller;

                public MethodInvoker SetResCaller
                {
                    get
                    {
                        if (setResCaller == null)
                        {
                            setResCaller = TCSType.GetMethod("SetResult", new Type[] { Type }).DelegateForCallMethod();
                        }
                        return setResCaller;
                    }
                }

                public MethodInvoker GetTaskCaller
                {
                    get
                    {
                        if (getTaskCaller == null)
                            getTaskCaller = TCSType.GetProperty("Task").GetMethod.DelegateForCallMethod();
                        return getTaskCaller;
                    }
                }

                public MethodInvoker SetExceptionCaller
                {
                    get
                    {
                        if (setExceptionCaller == null)
                            setExceptionCaller = TCSType.GetMethod("SetException", new Type[] { typeof(Exception) }).DelegateForCallMethod();
                        return setExceptionCaller;
                    }
                }

                private static readonly Dictionary<Type, TaskDelegateCacheContainer> tdcCache = new Dictionary<Type, TaskDelegateCacheContainer>();

                public object CreateTcs()
                {
                    return Activator.CreateInstance(TCSType);
                }

                public TaskDelegateCacheContainer(Type type)
                {
                    Type = type;
                    TCSType = typeof(TaskCompletionSource<>).MakeGenericType(type);
                }
            }


        }
        #endregion

        public static TaskGeneric TryGetAsGenericTask(this Task task)
        {
            if (Object.ReferenceEquals(null, task))
                throw new ArgumentNullException(nameof(task));
            return TaskGeneric.TryCreateFromTask(task);
        }

        [System.Diagnostics.DebuggerDisplay("Status = {Status}, Result = {ResultAsString}")]
        public class TaskGeneric
        {
            static readonly Dictionary<Type, TaskGenDelCache> genTaskDelCache = new Dictionary<Type, TaskGenDelCache>();

            TaskGeneric() { }

            public static TaskGeneric TryCreateFromTask(Task task)
            {
                var genTask = task.GetType();
                for (;;)
                {
                    if (genTask == typeof(Task))
                        return null;
                    if (genTask.IsGenericType)
                    {
                        var t = genTask.GetGenericTypeDefinition();
                        if (t == typeof(Task<>))
                            break;
                    }
                    genTask = genTask.BaseType;
                }
                var resType = genTask.GetGenericArguments()[0];
                if (!genTaskDelCache.TryGetValue(resType, out TaskGenDelCache tgdc))
                    genTaskDelCache[resType] = tgdc = new TaskGenDelCache(genTask);
                return new TaskGeneric
                {
                    Instance = task,
                    ResultType = resType,
                    cache = tgdc
                };
            }

            public Task Instance { get; private set; }

            TaskGenDelCache cache;

            public Type GenTaskType { get { return cache.GenTaskType; } }
            public Type ResultType { get; private set; }

            class TaskGenDelCache
            {
                public TaskGenDelCache(Type genTaskType) { GenTaskType = genTaskType; }
                public Type GenTaskType { get; private set; }

                private MethodInvoker getResCaller;

                public MethodInvoker ResultCaller
                {
                    get
                    {
                        if (getResCaller == null)
                            getResCaller = GenTaskType.GetProperty("Result").GetMethod.DelegateForCallMethod();
                        return getResCaller;
                    }
                }
            }

            string ResultAsString
            {
                get
                {
                    if ((Instance.Status & (TaskStatus.RanToCompletion)) != 0)
                        return "" + Result;
                    return "<value not available>";
                }
            }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public object Result { get { return cache.ResultCaller(Instance); } }
        }


    }
}
