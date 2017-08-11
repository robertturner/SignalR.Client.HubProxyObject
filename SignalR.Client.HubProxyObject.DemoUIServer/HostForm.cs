using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SignalR.Client.HubProxyObject.DemoUIServer
{
    public partial class HostForm : Form
    {
        static readonly string url = "http://localhost:1968";

        static Resolver resolver;
        

        public HostForm()
        {
            InitializeComponent();
            resolver = new Resolver();
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            await RestartServer();

        }

        IDisposable server;
        Task RestartServer()
        {
            return Task.Run(() =>
            {
                if (server != null)
                {
                    //Console.WriteLine("Server shutting down...");
                    server.Dispose();
                    System.Threading.Thread.Sleep(100);
                }
                server = WebApp.Start<Startup>(url);
                //Console.WriteLine("Server running!");
            });
        }

        class Resolver : IDependencyResolver
        {
            public DemoHub hub = new DemoHub();
            public void Dispose()
            {
                
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(DemoHub))
                    return hub;
                throw new NotImplementedException();
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                throw new NotImplementedException();
            }

            public void Register(Type serviceType, Func<object> activator)
            {
                throw new NotImplementedException();
            }

            public void Register(Type serviceType, IEnumerable<Func<object>> activators)
            {
                throw new NotImplementedException();
            }
        }

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                //app.UseCors(CorsOptions.AllowAll);
                app.MapSignalR(new HubConfiguration { Resolver = resolver });
            }
        }

    }
}
