using BassThatHz_ASIO_DSP_Processor.GUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_InputValidator
{
    [TestMethod]
    public void Validate_IsNumeric_NonNegative_AllowsDigitsAndDot()
    {
        var e = new KeyPressEventArgs('5');
        InputValidator.Validate_IsNumeric_NonNegative(e);
        Assert.IsFalse(e.Handled);
        e = new KeyPressEventArgs('.');
        InputValidator.Validate_IsNumeric_NonNegative(e);
        Assert.IsFalse(e.Handled);
        e = new KeyPressEventArgs('a');
        InputValidator.Validate_IsNumeric_NonNegative(e);
        Assert.IsTrue(e.Handled);
    }

    [TestMethod]
    public void Validate_IsNumeric_Negative_AllowsMinus()
    {
        var e = new KeyPressEventArgs('-');
        InputValidator.Validate_IsNumeric_Negative(e);
        Assert.IsFalse(e.Handled);
        e = new KeyPressEventArgs('1');
        InputValidator.Validate_IsNumeric_Negative(e);
        Assert.IsFalse(e.Handled);
        e = new KeyPressEventArgs('x');
        InputValidator.Validate_IsNumeric_Negative(e);
        Assert.IsTrue(e.Handled);
    }

    [TestMethod]
    public void LimitTo_ReasonableSizedNumber_EnforcesLimits()
    {
        Assert.AreEqual("0", InputValidator.LimitTo_ReasonableSizedNumber("", false));
        Assert.AreEqual("999999999", InputValidator.LimitTo_ReasonableSizedNumber("1000000000"));
        Assert.AreEqual("-999999999", InputValidator.LimitTo_ReasonableSizedNumber("-1000000000"));
        Assert.AreEqual("123456789", InputValidator.LimitTo_ReasonableSizedNumber("123456789012345"));
        Assert.AreEqual("0", InputValidator.LimitTo_ReasonableSizedNumber("notanumber"));
        Assert.AreEqual("", InputValidator.LimitTo_ReasonableSizedNumber("", true));
    }

    [TestMethod]
    public void Set_TextBox_MaxLength_SetsTo9()
    {
        var tb = new TextBox();
        InputValidator.Set_TextBox_MaxLength(tb);
        Assert.AreEqual(9, tb.MaxLength);
    }
}