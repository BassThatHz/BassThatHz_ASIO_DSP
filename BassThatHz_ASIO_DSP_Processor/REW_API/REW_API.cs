#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.REW_API;

#region Usings
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

public class REW_API
{
    public string REW_baseUrl = "http://localhost:4735";

    // Reuse HttpClient and JsonSerializerOptions to reduce allocations and improve perf
    private static readonly HttpClient s_httpClient = new HttpClient();
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task PostToREW_API(string REW_ID, REW_TargetSettings REW_TargetSettings, List<REW_Filter> REW_Filters)
    {
        // Serialize using shared options and reuse static HttpClient
        var targetSettingsJson = JsonSerializer.Serialize(REW_TargetSettings, s_jsonOptions);
        using var targetSettingsContent = new StringContent(targetSettingsJson, Encoding.UTF8, "application/json");
        var targetSettingsResponse = await s_httpClient.PostAsync(new Uri($"{REW_baseUrl}/measurements/{REW_ID}/target-settings"), targetSettingsContent).ConfigureAwait(false);
        _ = targetSettingsResponse.IsSuccessStatusCode;

        // Wrap filters into an object to avoid manual string concatenation
        var filtersWrapper = new { filters = REW_Filters };
        var filtersJson = JsonSerializer.Serialize(filtersWrapper, s_jsonOptions);
        using var filtersContent = new StringContent(filtersJson, Encoding.UTF8, "application/json");
        var filtersResponse = await s_httpClient.PostAsync(new Uri($"{REW_baseUrl}/measurements/{REW_ID}/filters"), filtersContent).ConfigureAwait(false);
        _ = filtersResponse.IsSuccessStatusCode;
    }

    public async Task<REW_TargetSettings?> GetTargetSettingsFromREW_API(string REW_ID)
    {
        var targetSettingsResponse = await s_httpClient.GetAsync(new Uri($"{REW_baseUrl}/measurements/{REW_ID}/target-settings")).ConfigureAwait(false);
        if (targetSettingsResponse.IsSuccessStatusCode)
        {
            string jsonContent = await targetSettingsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<REW_TargetSettings>(jsonContent, s_jsonOptions);
        }
        else
        {
            throw new Exception("REW target-settings Error Response Code: " + targetSettingsResponse.StatusCode.ToString());
        }
    }

    public async Task<List<REW_Filter>?> GetFiltersFromREW_API(string REW_ID)
    {
        var filtersResponse = await s_httpClient.GetAsync(new Uri($"{REW_baseUrl}/measurements/{REW_ID}/filters")).ConfigureAwait(false);
        if (filtersResponse.IsSuccessStatusCode)
        {
            string jsonContent = await filtersResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<REW_Filter>>(jsonContent, s_jsonOptions);
        }
        else
        {
            throw new Exception("REW target-settings Error Response Code: " + filtersResponse.StatusCode.ToString());
        }
    }

    public FilterTypes REW_To_FilterType(string input)
    {
        switch (input)
        {
            case "HS":
            case "HS Q":
            case "HS 6dB":
            case "HS 12dB":
                return FilterTypes.High_Shelf;
            case "LS":
            case "LS Q":
            case "LS 6dB":
            case "LS 12dB":
                return FilterTypes.Low_Shelf;
            case "HP":
            case "HP1":
            case "HP Q":
            case "High pass":
                return FilterTypes.Adv_High_Pass;
            case "LP":
            case "LP1":
            case "LP Q":
            case "Low pass":
                return FilterTypes.Adv_Low_Pass;
            case "All pass":
                return FilterTypes.All_Pass;
            case "Notch":
            case "Notch Q":
                return FilterTypes.Notch;
            case "PK":
                return FilterTypes.PEQ;
            default:
                return FilterTypes.PEQ;
        }
    }

    public string FilterTypeToREW(FilterTypes input)
    {
        switch (input)
        {
            case FilterTypes.High_Shelf:
                return "HS Q";
            case FilterTypes.Low_Shelf:
                return "LS Q";
            case FilterTypes.Adv_High_Pass:
                return "HP Q";
            case FilterTypes.Adv_Low_Pass:
                return "LP Q";
            case FilterTypes.All_Pass:
                return "All pass";
            case FilterTypes.Notch:
                return "Notch Q";
            case FilterTypes.PEQ:
                return "PK";
            default:
                return "PK";
        }
    }

    public Basic_HPF_LPF.FilterOrder REW_To_FilterOrder(string input)
    {
        switch (input)
        {
            case "L-R2":
                return Basic_HPF_LPF.FilterOrder.LR_12db;
            case "L-R4":
                return Basic_HPF_LPF.FilterOrder.LR_24db;
            case "L-R6":
            case "L-R8":
                return Basic_HPF_LPF.FilterOrder.LR_48db;

            case "BU1":
                return Basic_HPF_LPF.FilterOrder.BW_6db;
            case "BE2":
            case "BU2":
                return Basic_HPF_LPF.FilterOrder.BW_12db;
            case "BE3":
            case "BU3":
                return Basic_HPF_LPF.FilterOrder.BW_18db;
            case "BE4":
            case "BU4":
                return Basic_HPF_LPF.FilterOrder.BW_24db;
            case "BE5":
            case "BU5":
                return Basic_HPF_LPF.FilterOrder.BW_30db;
            case "BE6":
            case "BU6":
                return Basic_HPF_LPF.FilterOrder.BW_36db;
            case "BE7":
            case "BU7":
                return Basic_HPF_LPF.FilterOrder.BW_42db;
            case "BE8":
            case "BU8":
                return Basic_HPF_LPF.FilterOrder.BW_48db;

            default:
                return Basic_HPF_LPF.FilterOrder.None;
        }
    }

    public string FilterOrderToREW(Basic_HPF_LPF.FilterOrder? input)
    {
        if (input == null) return "L-R2";

        switch (input)
        {
            case Basic_HPF_LPF.FilterOrder.LR_12db:
                return "L-R2";
            case Basic_HPF_LPF.FilterOrder.LR_24db:
                return "L-R4";
            case Basic_HPF_LPF.FilterOrder.LR_48db:
                return "L-R8";

            case Basic_HPF_LPF.FilterOrder.BW_6db:
                return "BU1";
            case Basic_HPF_LPF.FilterOrder.BW_12db:
                return "BU2";
            case Basic_HPF_LPF.FilterOrder.BW_18db:
                return "BU3";
            case Basic_HPF_LPF.FilterOrder.BW_24db:
                return "BU4";
            case Basic_HPF_LPF.FilterOrder.BW_30db:
                return "BU5";
            case Basic_HPF_LPF.FilterOrder.BW_36db:
                return "BU6";
            case Basic_HPF_LPF.FilterOrder.BW_42db:
                return "BU7";
            case Basic_HPF_LPF.FilterOrder.BW_48db:
                return "BU8";

            default:
                return "L-R2";
        }
    }

    public class REW_Filter
    {
        public int index { get; set; }
        public string type { get; set; } = string.Empty;
        public bool enabled { get; set; }
        public bool isAuto { get; set; }
        public double frequency { get; set; }
        public double? gaindB { get; set; } = null;
        public double? q { get; set; } = null;
    }

    public class REW_TargetSettings
    {
        public string shape { get; set; } = string.Empty;
        public int? bassManagementSlopedBPerOctave { get; set; } = null;
        public int? bassManagementCutoffHz { get; set; } = null;
        public int? lowFreqSlopedBPerOctave { get; set; } = null;
        public int? lowFreqCutoffHz { get; set; } = null;
        public string lowPassCrossoverType { get; set; } = string.Empty;
        public string highPassCrossoverType { get; set; } = string.Empty;
        public int lowPassCutoffHz { get; set; }
        public int highPassCutoffHz { get; set; }
    }
}