namespace ConvivaScripts
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public enum MetricLensState
    {
        NA = -1,
        Disabled = 0,
        Enabled = 1,
    }

    public enum MetricLensStatus
    {
        NA = -1,
        OK = 0,
        Error = 1,
        WarmUp = 2,
        InvalidFilter = 3,
        InvalidDimension = 4,
        InvalidConfig = 5,
    }

    public class ConvivaFilterRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("request")]
        public Request Request { get; set; }
    }

    public class Request
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("subcategory")]
        public string Subcategory { get; set; }

        [JsonProperty("enabled")]
        public string Enabled { get; set; }

        [JsonProperty("advanced")]
        public bool Advanced { get; set; }

        [JsonProperty("rules")]
        public Rules Rules { get; set; }
    }

    public class Rules
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("rules")]
        public List<RulesRule> RulesRules { get; set; }
    }

    public class RulesRule
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("rules")]
        public List<Rule> Rules { get; set; }
    }

    public class Rule
    {
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key { get; set; }

        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class RulesSectionDefinition
    {
        public string Field { get; set; }

        public string Key { get; set; }

        public string Operation { get; set; }

        public string Value { get; set; }

        public string Group { get; set; }
    }

    public class ConvivaDomData
    {
        public string FilterName { get; set; }

        public string Category { get; set; }

        public string Subcategory { get; set; }

        public string Enabled { get; set; }

        public string Type { get; set; }

        public string ElementName { get; set; }

        public string InstanceId { get; set; }
    }

    public class ConvivaElementInfo
    {
        public const int FilterListener = 899;

        public const int MetricLensTable = 700;
        public const int FilterTable = 2400;
    }
}