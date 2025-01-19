namespace BassThatHz_ASIO_DSP_Processor.REW_API
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public static class REW_API
    {
        public static string REW_baseUrl = "http://localhost:4735";

        public static async Task PostToREW_API(string REW_ID, REW_TargetSettings REW_TargetSettings, List<REW_Filter> REW_Filters)
        {
            using HttpClient client = new();
            var TargetSettings_JSONContent = JsonSerializer.Serialize(REW_TargetSettings);
            var targetSettingsContent = new StringContent(TargetSettings_JSONContent, Encoding.UTF8, "application/json");
            var TargetSettingsResponse = await client.PostAsync($"{REW_baseUrl}/measurements/{REW_ID}/target-settings", targetSettingsContent);
            _ = TargetSettingsResponse.IsSuccessStatusCode;

            var Filters_JSONContent = JsonSerializer.Serialize(REW_Filters);
            Filters_JSONContent = "{\"filters\":" + Filters_JSONContent + "}";
            var Filters_Content = new StringContent(Filters_JSONContent, Encoding.UTF8, "application/json");
            var FiltersResponse = await client.PostAsync($"{REW_baseUrl}/measurements/{REW_ID}/filters", Filters_Content);
            _ = FiltersResponse.IsSuccessStatusCode;
        }

        public static async Task<REW_TargetSettings?> GetTargetSettingsFromREW_API(string REW_ID)
        {
            using HttpClient client = new();
            var TargetSettingsResponse = await client.GetAsync($"{REW_baseUrl}/measurements/{REW_ID}/target-settings");
            if (TargetSettingsResponse.IsSuccessStatusCode)
            {
                string JSONContent = await TargetSettingsResponse.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<REW_TargetSettings>(JSONContent);
            }
            else
            {
                throw new Exception("REW target-settings Error Response Code: " + TargetSettingsResponse.StatusCode.ToString());
            }
        }

        public static async Task<List<REW_Filter>?> GetFiltersFromREW_API(string REW_ID)
        {
            using HttpClient client = new();
            var FiltersResponse = await client.GetAsync($"{REW_baseUrl}/measurements/{REW_ID}/filters");
            if (FiltersResponse.IsSuccessStatusCode)
            {
                string JSONContent = await FiltersResponse.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<REW_Filter>>(JSONContent);
            }
            else
            {
                throw new Exception("REW target-settings Error Response Code: " + FiltersResponse.StatusCode.ToString());
            }
        }

        public static FilterTypes REW_To_FilterType(string input)
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

        public static string FilterTypeToREW(FilterTypes input)
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

        public static Basic_HPF_LPF.FilterOrder REW_To_FilterOrder(string input)
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

        public static string FilterOrderToREW(Basic_HPF_LPF.FilterOrder? input)
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
}