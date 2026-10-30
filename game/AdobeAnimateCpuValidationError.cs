using System.Text.Json.Serialization;

internal sealed record AdobeAnimateCpuValidationError([property: JsonPropertyOrder(0)] string ResourcePath, [property: JsonPropertyOrder(1)] string Clip, [property: JsonPropertyOrder(2)] int Frame, [property: JsonPropertyOrder(3)] string FailureCode, [property: JsonPropertyOrder(4)] string Detail);
