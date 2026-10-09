using System.Text.Json.Serialization;

namespace EnbMaps.Models;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(EnbMapsDataset))]
public sealed partial class EnbMapsJsonContext : JsonSerializerContext;
