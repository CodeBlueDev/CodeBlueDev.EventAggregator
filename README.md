# CodeBlueDev.EventAggregator
A publish/subscribe aggregator to be used to decouple objects.

## Subscribe
```cs
public class PingHandler
{
    public PingHandler()
    {
        this.Subscribe<Ping>(async pingEvent => Task.Run(() => this.WritePing()));
    }
    
    //...
}
```

## Publish
```cs
public class Pong 
{
    // ...
    public void SomeMethod()
    {
        this.Publish(new Ping().AsTask());
    }
    // ...
}
```

## Unsubscribe
```cs
public class PingHandler
{
    // ...
    public void Dispose()
    {
        this.Unsubscribe<Ping>();
    }
    // ...
}
```
