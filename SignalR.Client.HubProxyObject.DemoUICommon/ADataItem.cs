using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Client.HubProxyObject.DemoUICommon
{
    public class ADataItem : INotifyPropertyChanged
    {

        int anInt;
        public int AnInt
        {
            get => anInt;
            set { SetProp(ref anInt, value); }
        }

        int aUint;
        public int AUint
        {
            get => aUint;
            set { SetProp(ref aUint, value); }
        }

        string aString;
        public string AString
        {
            get => aString;
            set { SetProp(ref aString, value); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        void SetProp<T>(ref T dest, T newVal, [CallerMemberName]string prop = null)
        {
            dest = newVal;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

    }
}
