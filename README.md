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

## Note
This is but one way to use the library in a somewhat object-oriented manner. See the Console.Test project for another example. 

## Warning
If this is being used in a WinForms project, check for InvokeRequired before writing the handler code or you'll have a bad time. Exceptions will be thrown and you may not understand why.
