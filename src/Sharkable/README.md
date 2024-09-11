# Sharkable
a dotnet minimal api framework collection

##Usage
create a new class
```
//first add extension
using Sharkable
builder.Services.AddShark();
//for aot users please specify assemblies by youself and avoid code trim
build.Services.AddShark(typeof(Program).Assembly);

[ScopedService] //inject class as a scoped service by the given attribute
public class Monitor : IMonitor
{
    public void Show()
    {
        ...
    }
}

//map an endpoint and it works!
app.MapGet("/monitor",([FromServices]IMonitor monitor) =>
{
    monitor.Show();
});
```

for more use sample please see Sharkable.Sample project