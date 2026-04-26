#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI;

#region Usings
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
/// 
/// Permission is hereby granted to use this software 
/// and associated documentation files (the "Software"), 
/// for educational purposess, scientific purposess or private purposess
/// or as part of an open-source community project, 
/// (and NOT for commerical use or resale in substaintial part or whole without prior authorization)
/// and all copies of the Software subject to the following conditions:
/// 
/// The copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
public static class InputValidator
{
    public static void Validate_IsNumeric_NonNegative(KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            e.Handled = true;
    }

    public static void Validate_IsNumeric_Negative(KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
            e.Handled = true;
    }

    public static string LimitTo_ReasonableSizedNumber(string input, bool allowEmpty = false)
    {
        const int MaxChars = 9;
        const double MaxValue = 999_999_999d;
        const double MinValue = -999_999_999d;

        input ??= string.Empty;

        // Trim whitespace without allocating when input has no surrounding whitespace
        int start = 0, end = input.Length - 1;
        while (start <= end && char.IsWhiteSpace(input[start])) start++;
        while (end >= start && char.IsWhiteSpace(input[end])) end--;

        string trimmed = (start == 0 && end == input.Length - 1) ? input : input.Substring(start, end - start + 1);

        if (!allowEmpty && trimmed.Length == 0)
            return "0";

        if (trimmed.Length > MaxChars)
            trimmed = trimmed.Substring(0, MaxChars);

        if (double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double value))
        {
            if (value > MaxValue)
                return MaxValue.ToString(CultureInfo.InvariantCulture);
            if (value < MinValue)
                return MinValue.ToString(CultureInfo.InvariantCulture);

            return trimmed;
        }

        return allowEmpty ? string.Empty : "0";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set_TextBox_MaxLength(TextBox input)
    {
        const int MaxLength = 9;
        if (input.MaxLength != MaxLength)
            input.MaxLength = MaxLength;
    }
}