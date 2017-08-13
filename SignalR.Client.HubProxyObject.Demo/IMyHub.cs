using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.Demo
{
    public interface IMyHub
    {
        Task<string> AsyncMethod(string arg1);
        Task MethodWithNoReturn(int arg1, string arg2);
        void SyncMethodNoArgs();

        event Action<(string Arg1, int Arg2)> ASignal;

        event Action<(string Arg1, int Arg2)> Signal2;
    }
}
