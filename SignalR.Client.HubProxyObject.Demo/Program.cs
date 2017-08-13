using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalR.Client.HubProxyObject.ResilientConnection;

namespace SignalR.Client.HubProxyObject.Demo
{

    class Program
    {
        static readonly string url = "http://localhost:1968";
        static void Main(string[] args)
        {
            RestartServer();
            bool keepGoing = true;
            do
            {
                Console.WriteLine("Type Exit to quit; Restart to restart");
                var str = Console.ReadLine();
                switch (str)
                {
                    case "Exit": keepGoing = false; break;
                    case "Restart": RestartServer(); break;
                    case "Client": DoClient(); break;
                    case "Send":
                        using (var disp = cp.GetActiveConnection()
                            .Subscribe(con =>
                            {
                                con.HubProxies.SyncMethodNoArgs();
                            },
                            ex => { },
                            () => { }))
                        {
                            System.Threading.Thread.Sleep(300);
                        }
                        break;
                    case "Send2":
                        var tcs = new TaskCompletionSource<bool>();
                        using (var disp = cp.GetActiveConnection()
                            .Subscribe(con =>
                            {
                                con.HubProxies.AsyncMethod("bob")
                                .ContinueWith(t =>
                                    {
                                        Console.WriteLine("Async res: " + t.Result);
                                        tcs.TrySetResult(true);
                                    });
                            },
                            ex => { },
                            () => { }))
                        {
                            tcs.Task.Wait();
                            System.Threading.Thread.Sleep(300);
                        }
                        break;
                }
            }
            while (keepGoing);

        }

        static IDisposable server;
        static void RestartServer()
        {
            if (server != null)
            {
                Console.WriteLine("Server shutting down...");
                server.Dispose();
                System.Threading.Thread.Sleep(100);
            }
            server = WebApp.Start<Startup>(url);
            Console.WriteLine("Server running!");
        }

        static IObservable<(string arg, int num)> ASigStream()
        {
            return cp.GetResilientStream(connection =>
            {
                return ASignalForCon(connection.HubProxies);
            }, TimeSpan.FromSeconds(20));
        }

        static IObservable<(string arg, int num)> ASignalForCon(IMyHub con)
        {
            return Observable.Create<(string arg, int num)>(observer =>
            {
                con.ASignal += observer.OnNext;
                return Disposable.Create(() => con.ASignal -= observer.OnNext);
            })
            .Publish().RefCount();
        }

        static IConnectionProvider<IMyHub> cp;

        static void DoClient()
        {
            try
            {
#if false
                using (var signalR = new HubConnection(url))
                {
                    IMyHub myHubProxy = signalR.CreateProxy<IMyHub>("myHub");
                    var baseProxy = (IHubProxy)myHubProxy;
                    await signalR.Start();

                    myHubProxy.ASignal.On += arg =>
                    {

                    };


                    string result = await myHubProxy.AsyncMethod("an arg");
                    await myHubProxy.MethodWithNoReturn(4, "arg2");
                    myHubProxy.SyncMethodNoArgs(); // will block on method call

                    Console.ReadLine();
                }
#else
                cp = new ConnectionProvider<IMyHub>(url, sigR => sigR.CreateProxy<IMyHub>("myHub"));

                cp.GetActiveConnection().Select(con => con.StatusStream).Switch().Publish().RefCount()
                    .Subscribe(status => 
                    {
                        Console.WriteLine("Client connection status change: {0}", status);
                    });

                var evs = Observable.Defer(() => ASigStream())
                    .Catch(Observable.Return(("EXCEPTION", -1)))
                    .Repeat()
                    .Publish().RefCount();

                evs.Subscribe(val =>
                {
                    Console.WriteLine("On: " + val.Item1 + ", val: " + val.Item2.ToString());
                },
                ex => { },
                () => { });

#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
            }
        }


        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                //app.UseCors(CorsOptions.AllowAll);
                app.MapSignalR(new HubConfiguration());
            }
        }

    }
}
