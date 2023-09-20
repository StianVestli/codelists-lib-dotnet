using System.Text.Json.Serialization;

namespace Altinn.Codelists.SSB.Clients;

/// <summary>
/// List of classification codes from a specified classification.
/// </summary>
public class CorrespondenceClassificationCodes
{
    /// <summary>
    /// correspondence mappings between a source classification and a target classification.
    /// </summary>
    [JsonPropertyName("correspondenceItems")]
    public List<CorrespondenceClassificationCode> CorrespondenceItems { get; set; } = new List<CorrespondenceClassificationCode>();
}
