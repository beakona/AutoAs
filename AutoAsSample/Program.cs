using BeaKona;
using System.Collections;

namespace AutoAsSample;

internal partial class Program
{
    static void Main()
    {
        //System.Diagnostics.Debug.WriteLine(BeaKona.Output.Debug_TestClass_1.Info);

        TestClass<int> t = new TestClass<int>();
        var x = t.AsMy1();
    }
}

public interface IMy1Base
{
}

public interface IMy1<H> : IMy1Base
{
}

internal interface IMy2<T>
{
}

internal interface IMy2<T1, T2>
{
}

internal interface IMy3
{
}

internal interface @internal
{
}

public abstract class TestClassBase : IMy3
{
}

[GenerateAutoAs(EntireInterfaceHierarchy = false, SkipSystemInterfaces = true)]
public partial class TestClass<T> : TestClassBase, IMy1<T>, IMy2<int>, IMy2<string>, IMy2<string, string>, IEnumerable<int>, @internal
{
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    //internal IMy2<int> AsMy2_0() => this;
}
