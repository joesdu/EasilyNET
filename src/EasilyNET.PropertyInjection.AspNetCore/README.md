#### EasilyNET.PropertyInjection.AspNetCore


```csharp
//添加 AddPropertyInjectionAsServices
builder.Services.AddControllers().AddPropertyInjectionAsServices();

...

//添加
builder.Host.UseDefaultPropertyInjection();

...

var app = builder.Build();
```