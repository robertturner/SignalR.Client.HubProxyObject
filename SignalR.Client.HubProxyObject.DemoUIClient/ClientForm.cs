using SignalR.Client.HubProxyObject.DemoUICommon;
using SignalR.Client.HubProxyObject.ResilientConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Linq;

namespace SignalR.Client.HubProxyObject.DemoUIClient
{
    public partial class ClientForm : Form
    {
        static readonly string url = "http://localhost:1968";

        public ClientForm()
        {
            InitializeComponent();
        }

        ConnectionProvider<IDemoHub> cp;

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            cp = new ConnectionProvider<IDemoHub>(url, sigR => sigR.CreateProxy<IDemoHub>("demoHub"));
            cp.GetActiveConnection().Subscribe(con => con.HubProxies.ASig += 
            arg => 
            {
            });

            //BindingListHubWrapper.GetProxy<string, ADataItem>()

        }
    }
}
