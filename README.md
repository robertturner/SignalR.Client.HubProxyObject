# SignalR.Client.HubProxyObject
Client interface proxy for SignalR. Proxies interface methods for a SignalR Hub.

## Example

```
public interface IMyHub
{
  Task<string> AsyncMethod(string arg1);
  Task MethodWithNoReturn(int arg1, string arg2);
  void SyncMethodNoArgs();
}
```
Create proxy and start the client:
```
var signalR = new HubConnection(url);
IMyHub myHubProxy = signalR.CreateProxy<IMyHub>("myHub");
await signalR.Start();
```
Super happy place:
```
string result = await myHubProxy.AsyncMethod("an arg");
...
await myHubProxy.MethodWithNoReturn(4, "arg2");
...
myHubProxy.SyncMethodNoArgs(); // will block on method call
```
## Server to Client calls
Add HubSignal property to Hub interface:
```
public interface IMyHub
{
	HubSignal<string> ASignal { get; }
}
```
Subscribe to the "On" event at the client:
```
myHubProxy.ASignal.On += arg => { };
```
At server end, implement interface with a private set method, call HubSignal.ImplementSignals before using, then call All, Others, or Caller:
```
public class MyHub : Hub, IMyHub
{
    public MyHub()
    {
        HubSignal.ImplementSignals(this);
    }
	public HubSignal<string> ASignal { get; private set; }
	...
}
...
hubInstance.ASignal.Others("an arg");
```
