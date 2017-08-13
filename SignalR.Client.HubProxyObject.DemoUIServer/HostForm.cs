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

        

        public HostForm()
        {
            InitializeComponent();
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

        

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                //app.UseCors(CorsOptions.AllowAll);
                app.MapSignalR(new HubConfiguration());
            }
        }

        private void buttonCallSig_Click(object sender, EventArgs e)
        {
            GlobalHost.ConnectionManager.GetHubContext("demoHub").Clients.All.ASig("bob");
        }
    }
}
