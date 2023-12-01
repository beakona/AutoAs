# AutoAs

C# [Source Generator](https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.md) which generates fluent AsInterface cast-methods for all interfaces type implements.
<br>

Manually written source:

```csharp

public interface IPrintable
{
}

[BeaKona.AutoAs]
public partial class Person : IPrintable
{
}
```

Auto-generated accompanying source:

```csharp
partial class Person
{
   public IPrintable AsPrintable() => this;
}
```
<br>

Other examples can be found in [wiki](https://github.com/beakona/AutoAs/wiki/Examples).

<br>
---

![.NET Core](https://github.com/beakona/AutoAs/workflows/.NET%20Core/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/BeaKona.AutoAsGenerator)](https://www.nuget.org/packages/BeaKona.AutoAsGenerator)
