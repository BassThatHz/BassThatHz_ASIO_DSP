namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;

// IHasFocus is a marker-style interface with a single method, HasFocus(), and carries no
// default implementation or other logic of its own (see
// BassThatHz_ASIO_DSP_Processor\GUI\Tabs\IHasFocus.cs). There is no behavior to unit test on
// the interface itself, so these tests instead verify its shape/contract via reflection (so a
// future accidental signature change would be caught) and verify the contract is honored by a
// minimal implementing class, which is the most meaningful test possible for a pure interface.
[TestClass]
public class Test_IHasFocus
{
    private class MinimalHasFocusImplementation : IHasFocus
    {
        public int CallCount { get; private set; }

        public void HasFocus()
        {
            CallCount++;
        }
    }

    [TestMethod]
    public void IHasFocus_IsAnInterfaceType()
    {
        Assert.IsTrue(typeof(IHasFocus).IsInterface);
    }

    [TestMethod]
    public void IHasFocus_DeclaresExactlyOneMember_HasFocus()
    {
        var members = typeof(IHasFocus).GetMethods();
        Assert.AreEqual(1, members.Length);
        Assert.AreEqual("HasFocus", members[0].Name);
    }

    [TestMethod]
    public void IHasFocus_HasFocusMethod_TakesNoParametersAndReturnsVoid()
    {
        var method = typeof(IHasFocus).GetMethod("HasFocus");
        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(void), method!.ReturnType);
        Assert.AreEqual(0, method.GetParameters().Length);
    }

    [TestMethod]
    public void MinimalImplementation_CanBeAssignedToInterfaceType()
    {
        IHasFocus instance = new MinimalHasFocusImplementation();
        Assert.IsNotNull(instance);
    }

    [TestMethod]
    public void MinimalImplementation_HasFocus_InvokesImplementation()
    {
        var impl = new MinimalHasFocusImplementation();
        IHasFocus asInterface = impl;

        asInterface.HasFocus();

        Assert.AreEqual(1, impl.CallCount);
    }

    [TestMethod]
    public void MinimalImplementation_HasFocus_CanBeCalledMultipleTimes()
    {
        var impl = new MinimalHasFocusImplementation();
        IHasFocus asInterface = impl;

        asInterface.HasFocus();
        asInterface.HasFocus();
        asInterface.HasFocus();

        Assert.AreEqual(3, impl.CallCount);
    }

    [TestMethod]
    public void KnownProductionTypes_ImplementIHasFocus_AsExpected()
    {
        // Spot-check that real GUI tab controls in the assembly declare IHasFocus, confirming
        // the interface is actually used as intended across the codebase rather than being
        // dead code.
        var assembly = typeof(IHasFocus).Assembly;
        var implementingTypes = assembly.GetTypes()
            .Where(t => typeof(IHasFocus).IsAssignableFrom(t) && t != typeof(IHasFocus))
            .ToList();

        Assert.IsTrue(implementingTypes.Count > 0, "Expected at least one production type to implement IHasFocus.");
    }
}
