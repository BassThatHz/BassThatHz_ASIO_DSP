using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test_Project_1;

/// <summary>
/// Exposes the protected chart/list/checkbox members that the Component and Individual traces are
/// driven from, so the drawing can be asserted on without showing the form.
/// </summary>
public class TestableFormGPEQ : FormGPEQ
{
    public Chart Chart => this.GPEQ_Chart;
    public ListBox FilterList => this.Filters_LSB;
    public CheckBox TotalMag => this.ShowTotalMag_CHK;
    public CheckBox ComponentMag => this.ShowComponentMag_CHK;
    public CheckBox ComponentPhase => this.ShowComponentPhase_CHK;
    public CheckBox IndividualMag => this.ShowIndividualMag_CHK;
    public CheckBox IndividualPhase => this.ShowIndividualPhase_CHK;

    public IReadOnlyList<Series> MagTraces =>
        this.Chart.Series.Where(s => s.Name.StartsWith("Filter_Mag_")).ToList();

    public IReadOnlyList<Series> PhaseTraces =>
        this.Chart.Series.Where(s => s.Name.StartsWith("Filter_Phase_")).ToList();
}

[TestClass]
[DoNotParallelize]
public class Test_FormGPEQ_PerFilterTraces
{
    #region Setup
    // FormGPEQ measures against the PROCESS-WIDE Program.DSP_Info.InSampleRate, which is 0 in a bare
    // test host and would leave every response flat. Set it, then hand it back.
    protected int SavedSampleRate;

    [TestInitialize]
    public void Setup()
    {
        this.SavedSampleRate = Program.DSP_Info.InSampleRate;
        Program.DSP_Info.InSampleRate = 48000;
    }

    [TestCleanup]
    public void Cleanup()
    {
        Program.DSP_Info.InSampleRate = this.SavedSampleRate;
    }
    #endregion

    #region Helpers
    protected const double Hz100 = 100;
    protected const double Hz1000 = 1000;

    protected static BiQuadFilter MakePEQ(double frequency, double q, double gain, bool enabled = true)
    {
        var Filter = new BiQuadFilter();
        Filter.PeakingEQ(Program.DSP_Info.InSampleRate, frequency, q, gain);
        Filter.FilterEnabled = enabled;
        return Filter;
    }

    protected static TestableFormGPEQ MakeForm(params IFilter[] filters)
    {
        var Form = new TestableFormGPEQ();
        Form.SetFilters(new List<IFilter>(filters));
        return Form;
    }

    /// <summary>
    /// Reads a trace's magnitude/phase at the point nearest to the requested frequency.
    /// </summary>
    protected static double ValueAt(Series series, double frequency)
    {
        DataPoint? Nearest = null;
        foreach (var Point in series.Points)
        {
            if (Nearest == null || Math.Abs(Point.XValue - frequency) < Math.Abs(Nearest.XValue - frequency))
                Nearest = Point;
        }

        Assert.IsNotNull(Nearest, "The trace had no points.");
        return Nearest.YValues[0];
    }
    #endregion

    [TestMethod]
    public void NoBoxesTicked_DrawsNoPerFilterTraces()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10));

        Assert.AreEqual(0, Form.MagTraces.Count);
        Assert.AreEqual(0, Form.PhaseTraces.Count);
    }

    [TestMethod]
    public void ComponentMag_DrawsOneTracePerEnabledFilter()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10));

        Form.ComponentMag.Checked = true;

        Assert.AreEqual(2, Form.MagTraces.Count, "One magnitude trace per filter was expected.");
        Assert.AreEqual(0, Form.PhaseTraces.Count, "Component Phase was not ticked.");
    }

    [TestMethod]
    public void ComponentMagAndPhase_DrawsBothTracesPerFilter()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10), new Basic_HPF_LPF() { FilterEnabled = true });

        Form.ComponentMag.Checked = true;
        Form.ComponentPhase.Checked = true;

        Assert.AreEqual(3, Form.MagTraces.Count);
        Assert.AreEqual(3, Form.PhaseTraces.Count);
    }

    [TestMethod]
    public void ComponentTraces_UseADistinctColourPerTrace()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10), MakePEQ(5000, 2, 3));

        Form.ComponentMag.Checked = true;
        Form.ComponentPhase.Checked = true;

        // Every line on the chart is its own colour, magnitude and phase of the same filter included.
        var Colours = Form.MagTraces.Concat(Form.PhaseTraces).Select(s => s.Color).ToList();
        Assert.AreEqual(6, Colours.Count);
        Assert.AreEqual(6, Colours.Distinct().Count(), "Every component trace should get its own colour.");

        for (int i = 0; i < Form.MagTraces.Count; i++)
        {
            Assert.AreNotEqual(Form.MagTraces[i].Color, Form.PhaseTraces[i].Color,
                "A filter's mag and phase must not share a colour.");
        }

        foreach (var Colour in Colours)
        {
            // The total traces are blue and red, so no component trace should land on either.
            var Hue = Colour.GetHue();
            Assert.IsTrue(Math.Min(Hue, 360 - Hue) > 15, "Colour too close to the total phase's red: hue " + Hue);
            Assert.IsTrue(Math.Abs(Hue - 240) > 15, "Colour too close to the total mag's blue: hue " + Hue);
        }
    }

    [TestMethod]
    public void ComponentTraces_ColoursAreSpacedApartFromEachOther()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10), MakePEQ(5000, 2, 3));

        Form.ComponentMag.Checked = true;
        Form.ComponentPhase.Checked = true;

        var Hues = Form.MagTraces.Concat(Form.PhaseTraces).Select(s => (double)s.Color.GetHue()).ToList();
        for (int i = 0; i < Hues.Count; i++)
        {
            for (int j = i + 1; j < Hues.Count; j++)
            {
                double Difference = Math.Abs(Hues[i] - Hues[j]);
                if (Difference > 180)
                    Difference = 360 - Difference;

                // Six traces over the ~280 degrees of usable hue: 25 degrees apart is not reachable,
                // but they should still be clearly separated rather than near duplicates.
                Assert.IsTrue(Difference > 20, "Two traces came out nearly the same colour: " + Hues[i] + " and " + Hues[j]);
            }
        }
    }

    [TestMethod]
    public void ComponentTraces_KeepTheirColoursAcrossRedraws()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10));

        Form.ComponentMag.Checked = true;
        Form.ComponentPhase.Checked = true;
        var MagBefore = Form.MagTraces.Select(s => s.Color).ToList();
        var PhaseBefore = Form.PhaseTraces.Select(s => s.Color).ToList();

        // Force the per-filter traces to be thrown away and rebuilt from scratch.
        Form.ComponentPhase.Checked = false;
        Form.ComponentPhase.Checked = true;

        CollectionAssert.AreEqual(MagBefore, Form.MagTraces.Select(s => s.Color).ToList());
        CollectionAssert.AreEqual(PhaseBefore, Form.PhaseTraces.Select(s => s.Color).ToList());
    }

    [TestMethod]
    public void ComponentMag_ShowsEachFiltersOwnContributionNotTheRunningTotal()
    {
        // Two boosts at different frequencies. A running total would show both bumps on the second
        // trace; a per-filter contribution shows only its own.
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.ComponentMag.Checked = true;

        var First = Form.MagTraces[0];
        var Second = Form.MagTraces[1];

        Assert.AreEqual(6.0, ValueAt(First, Hz100), 0.75, "Trace 1 should carry its own +6dB at 100Hz.");
        Assert.AreEqual(0.0, ValueAt(First, Hz1000), 0.75, "Trace 1 should be flat where filter 2 works.");

        Assert.AreEqual(-10.0, ValueAt(Second, Hz1000), 0.75, "Trace 2 should carry its own -10dB at 1000Hz.");
        Assert.AreEqual(0.0, ValueAt(Second, Hz100), 0.75, "Trace 2 must not include filter 1's boost.");
    }

    [TestMethod]
    public void DisabledFilter_IsNotDrawn()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 10, -10, enabled: false));

        Form.ComponentMag.Checked = true;

        Assert.AreEqual(1, Form.MagTraces.Count, "A disabled filter contributes nothing, so it is not drawn.");
    }

    [TestMethod]
    public void IndividualMag_DrawsOnlyTheSelectedFilter()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.FilterList.SelectedIndex = 1;
        Form.IndividualMag.Checked = true;

        Assert.AreEqual(1, Form.MagTraces.Count);
        Assert.AreEqual(-10.0, ValueAt(Form.MagTraces[0], Hz1000), 0.75);
    }

    [TestMethod]
    public void IndividualMag_FollowsTheListBoxSelection()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.FilterList.SelectedIndex = 0;
        Form.IndividualMag.Checked = true;
        Assert.AreEqual(6.0, ValueAt(Form.MagTraces[0], Hz100), 0.75);

        Form.FilterList.SelectedIndex = 1;
        Assert.AreEqual(1, Form.MagTraces.Count, "Still exactly one trace after the selection changed.");
        Assert.AreEqual(-10.0, ValueAt(Form.MagTraces[0], Hz1000), 0.75, "The trace should follow the new selection.");
    }

    [TestMethod]
    public void IndividualPhase_DrawsOnlyTheSelectedFilter()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.FilterList.SelectedIndex = 1;
        Form.IndividualPhase.Checked = true;

        Assert.AreEqual(0, Form.MagTraces.Count);
        Assert.AreEqual(1, Form.PhaseTraces.Count);
    }

    [TestMethod]
    public void IndividualMag_IsSuppressedWhileComponentMagIsTicked()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.FilterList.SelectedIndex = 1;
        Form.IndividualMag.Checked = true;
        Form.ComponentMag.Checked = true;

        Assert.AreEqual(2, Form.MagTraces.Count, "Component wins: one trace per filter, no extra individual trace.");

        // Individual comes back on its own once Component is cleared.
        Form.ComponentMag.Checked = false;
        Assert.AreEqual(1, Form.MagTraces.Count);
    }

    [TestMethod]
    public void IndividualPhase_IsSuppressedWhileComponentPhaseIsTicked()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.FilterList.SelectedIndex = 0;
        Form.IndividualPhase.Checked = true;
        Form.ComponentPhase.Checked = true;

        Assert.AreEqual(2, Form.PhaseTraces.Count);

        Form.ComponentPhase.Checked = false;
        Assert.AreEqual(1, Form.PhaseTraces.Count);
    }

    [TestMethod]
    public void UntickingTheBoxes_RemovesThePerFilterTraces()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        Form.ComponentMag.Checked = true;
        Form.ComponentPhase.Checked = true;
        Assert.AreEqual(4, Form.MagTraces.Count + Form.PhaseTraces.Count);

        Form.ComponentMag.Checked = false;
        Form.ComponentPhase.Checked = false;

        Assert.AreEqual(0, Form.MagTraces.Count);
        Assert.AreEqual(0, Form.PhaseTraces.Count);
        Assert.IsNotNull(Form.Chart.Series.FindByName("Series1"), "The total traces must survive.");
        Assert.IsNotNull(Form.Chart.Series.FindByName("Series2"));
    }

    [TestMethod]
    public void TotalTrace_IsUnchangedByThePerFilterTraces()
    {
        using var Form = MakeForm(MakePEQ(Hz100, 4, 6), MakePEQ(Hz1000, 4, -10));

        var TotalBefore = Form.Chart.Series["Series1"].Points.Select(p => p.YValues[0]).ToList();

        Form.ComponentMag.Checked = true;
        Form.ComponentPhase.Checked = true;

        var TotalAfter = Form.Chart.Series["Series1"].Points.Select(p => p.YValues[0]).ToList();

        Assert.AreEqual(TotalBefore.Count, TotalAfter.Count);
        for (int i = 0; i < TotalBefore.Count; i++)
            Assert.AreEqual(TotalBefore[i], TotalAfter[i], 0.001, "Measuring the parts must not disturb the total.");
    }
}
