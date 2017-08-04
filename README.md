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
Create a hub proxy and start the client:
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
## Todo
- Document Signal subscription model



