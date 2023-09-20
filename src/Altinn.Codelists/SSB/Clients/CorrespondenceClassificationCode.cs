using System.Text.Json.Serialization;

namespace Altinn.Codelists.SSB.Clients;

/// <summary>
/// Represents a single classification code.
/// </summary>
public class CorrespondenceClassificationCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CorrespondenceClassificationCode"/> class.
    /// </summary>
    public CorrespondenceClassificationCode(string sourceCode, string sourceName)
    {
        SourceCode = sourceCode;
        SourceName = sourceName;
    }

    /// <summary>
    /// Unique ID to the source classification
    /// </summary>
    [JsonPropertyName("sourceCode")]
    public string SourceCode { get; set; }

    /// <summary>
    /// Name of the source classification.
    /// </summary>
    [JsonPropertyName("sourceName")]
    public string SourceName { get; set; }

    /// <summary>
    /// Short name of the source classification.
    /// </summary>
    [JsonPropertyName("sourceShortName")]
    public string SourceShortName { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID to the target classification
    /// </summary>
    [JsonPropertyName("targetCode")]
    public string TargetCode { get; set; } = string.Empty;

    /// <summary>
    /// Name of the target classification.
    /// </summary>
    [JsonPropertyName("targetName")]
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Short name of the target classification.
    /// </summary>
    [JsonPropertyName("targetShortName")]
    public string TargetShortName { get; set; } = string.Empty;
}
