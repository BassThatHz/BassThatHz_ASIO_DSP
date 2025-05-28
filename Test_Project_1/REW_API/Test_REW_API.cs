#nullable enable

using BassThatHz_ASIO_DSP_Processor.REW_API;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace Test_Project_1;

[TestClass]
public class Test_REW_API
{
    [TestMethod]
    public async Task PostToREW_API_ShouldNotThrow()
    {
        var api = new REW_API();
        var targetSettings = new REW_API.REW_TargetSettings
        {
            shape = "Driver",
            lowPassCutoffHz = 2000,
            highPassCutoffHz = 20,
            lowPassCrossoverType = "L-R2",
            highPassCrossoverType = "L-R2"
        };
        var filters = new List<REW_API.REW_Filter>
        {
            new REW_API.REW_Filter
            {
                index = 1,
                type = "PK",
                enabled = true,
                isAuto = false,
                frequency = 1000,
                gaindB = 3.0,
                q = 1.0
            }
        };
        await api.PostToREW_API("test_id", targetSettings, filters);
    }

    [TestMethod]
    public async Task GetTargetSettingsFromREW_API_ShouldThrowOnInvalidId()
    {
        var api = new REW_API();
        await Assert.ThrowsExceptionAsync<System.Exception>(() => api.GetTargetSettingsFromREW_API("invalid_id"));
    }

    [TestMethod]
    public async Task GetFiltersFromREW_API_ShouldThrowOnInvalidId()
    {
        var api = new REW_API();
        await Assert.ThrowsExceptionAsync<System.Exception>(() => api.GetFiltersFromREW_API("invalid_id"));
    }

    [TestMethod]
    public void FilterTypeConversions_ShouldWork()
    {
        var api = new REW_API();
        Assert.AreEqual("HS Q", api.FilterTypeToREW(BassThatHz_ASIO_DSP_Processor.FilterTypes.High_Shelf));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.High_Shelf, api.REW_To_FilterType("HS Q"));
    }

    [TestMethod]
    public void FilterOrderConversions_ShouldWork()
    {
        var api = new REW_API();
        Assert.AreEqual("L-R2", api.FilterOrderToREW(BassThatHz_ASIO_DSP_Processor.Basic_HPF_LPF.FilterOrder.LR_12db));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.Basic_HPF_LPF.FilterOrder.LR_12db, api.REW_To_FilterOrder("L-R2"));
    }

    [TestMethod]
    public void FilterType_AllCases_And_Default()
    {
        var api = new REW_API();
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.High_Shelf, api.REW_To_FilterType("HS"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.Low_Shelf, api.REW_To_FilterType("LS 12dB"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.Adv_High_Pass, api.REW_To_FilterType("HP Q"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.Adv_Low_Pass, api.REW_To_FilterType("Low pass"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.All_Pass, api.REW_To_FilterType("All pass"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.Notch, api.REW_To_FilterType("Notch Q"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.PEQ, api.REW_To_FilterType("PK"));
        Assert.AreEqual(BassThatHz_ASIO_DSP_Processor.FilterTypes.PEQ, api.REW_To_FilterType("unknown")); // default
    }
}