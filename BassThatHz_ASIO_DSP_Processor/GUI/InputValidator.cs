#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI
{
    using System.Windows.Forms;

    /// <summary>
    ///  BassThatHz ASIO DSP Processor Engine
    ///  Copyright (c) 2025 BassThatHz
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
    public class InputValidator
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
            var ReturnValue = input;

            if (!allowEmpty && string.IsNullOrEmpty(ReturnValue))
                ReturnValue = "0";

            ReturnValue = ReturnValue.Trim();

            if (ReturnValue.Length > 9)
                ReturnValue = ReturnValue.Substring(0, 9);

            if (double.TryParse(ReturnValue, out double result))
            {
                if (result > 999999999) //Limit to 999 million
                    ReturnValue = "999999999";
                else if(result < -999999999) //Limit to -999 million
                    ReturnValue = "-999999999";
            }
            else
                if (!allowEmpty)
                    ReturnValue = "0"; //If not a number set to zero

            return ReturnValue;
        }

        public static void Set_TextBox_MaxLength(TextBox input)
        {
            input.MaxLength = 9;
        }
    }
}