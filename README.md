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
IMyHub myHubProxy = signalR.CreateProxy<ITestHub>("myHub");
await signalR.Start();
```
Then awesomeness:
```
string result = await myHubProxy.AsyncMethod("an arg");
...
await myHubProxy.MethodWithNoReturn(4, "arg2");
...
myHubProxy.SyncMethodNoArgs(); // will block on method call
```
## Usage notes




