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



