using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo
{

    class Program
    {
        static readonly string url = "http://localhost:1968";
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Server running!");

                DoClient().Wait();

            }
            Console.ReadLine();
        }

        static async Task DoClient()
        {
            try
            {
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
            }
            catch (Exception ex)
            {

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
