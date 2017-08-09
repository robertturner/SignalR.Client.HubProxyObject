﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo.ResilientConnection
{
    public static class Observable_Extensions
    {
        public static IObservable<TSource> TakeUntilInclusive<TSource>(this IObservable<TSource> source, Func<TSource, Boolean> predicate)
        {
            return Observable.Create<TSource>(observer => source.Subscribe(item =>
                {
                    observer.OnNext(item);
                    if (predicate(item))
                        observer.OnCompleted();
                },
                observer.OnError,
                observer.OnCompleted)
            );
        }

        public static IObservable<T> LazilyConnect<T>(this IConnectableObservable<T> connectable, SingleAssignmentDisposable futureDisposable)
        {
            var connected = 0;
            return Observable.Create<T>(observer =>
            {
                var subscription = connectable.Subscribe(observer);
                if (Interlocked.CompareExchange(ref connected, 1, 0) == 0)
                {
                    if (!futureDisposable.IsDisposed)
                    {
                        futureDisposable.Disposable = connectable.Connect();
                    }
                }
                return subscription;
            }).AsObservable();
        }
    }
}