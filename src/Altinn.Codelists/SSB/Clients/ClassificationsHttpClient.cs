using Altinn.Codelists.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;

namespace Altinn.Codelists.SSB.Clients;

/// <summary>
/// Http client to get classification codes from SSB.
/// </summary>
public class ClassificationsHttpClient : IClassificationsClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationsHttpClient"/> class.
    /// </summary>
    public ClassificationsHttpClient(IOptions<ClassificationSettings> settings, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(settings.Value.BaseApiUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;charset=utf-8");
    }

    /// <summary>
    /// Gets the codes for the specified classification.
    /// </summary>
    /// <param name="classificationId">The id of the classification to get</param>
    /// <param name="language">The language code used for the labels. Valid options are nb (norsk bokmål), nn (nynorsk) and en (english)
    /// Default if nothing is specified is nb (norsk bokmål).
    /// </param>
    /// <param name="atDate">The date the classification should be valid</param>
    /// <param name="level">The hierarchy level for classifications with multiple levels. Defaults to empty string, ie. all levels.</param>
    /// <param name="variant">The name of the variant to use instead of the original code list specified.</param>
    /// <param name="selectCodes">selectCodes is used to limit the result to codes that match the pattern given by selectCodes.</param>
    /// <param name="targetClassificationId">targetClassificationId specifies id of target classification used to get correspondence mappings between a source classification and a target classification.</param>
    /// <param name="concateChildren">specifies id of target child should be returned group by parent id</param>
    /// <returns></returns>
    public async Task<ClassificationCodes> GetClassificationCodes(int classificationId, string language = "nb", DateOnly? atDate = null, string level = "", string variant = "", string selectCodes = "", string targetClassificationId = "", string concateChildren = "")
    {
        string selectLanguage = $"language={language}";

        // If no date is specified we use todays date to get the latest classification codes.
        DateOnly date = atDate ?? DateOnly.FromDateTime(DateTime.Today);
        string selectDate = $"&date={date.ToString("yyyy-MM-dd")}";

        // No level specified means all levels will be returned
        string selectLevel = level == string.Empty ? string.Empty : $"&selectLevel={level}";

        // Variants are referenced by name
        string selectVariant = variant.IsNullOrEmpty() ? string.Empty : $"&variantName={variant}";

        //SelectCodes
        string selectedCodes = selectCodes.IsNullOrEmpty() ? string.Empty : $"&selectCodes={selectCodes}";

        //targetClassificationId if set, its a corresponding tabel
        string selectedTargetClassificationId = targetClassificationId.IsNullOrEmpty() ? string.Empty : $"&targetClassificationId={targetClassificationId}";


        // Start of url differs depending on if we are getting codes or variants
        string url = $"{classificationId}/codesAt";
        if (!variant.IsNullOrEmpty())
        {
            url = $"{classificationId}/variantAt";
        }
        //Corresponding table uses different stasrt of url
        if (!selectedTargetClassificationId.IsNullOrEmpty())
        {
            url = $"{classificationId}/correspondsAt";   
        }


        string query = BuildQuery(selectLanguage, selectDate, selectLevel, selectVariant, selectedCodes, selectedTargetClassificationId);

        var response = await _httpClient.GetAsync($"{url}{query}");

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!selectedTargetClassificationId.IsNullOrEmpty())
            {
                var corrClassificationCodes=BuildClassificationCodes(responseJson,concateChildren);
                return corrClassificationCodes ?? new ClassificationCodes();    
            }
            
            var classificationCodes = JsonSerializer.Deserialize<ClassificationCodes>(responseJson);
            return classificationCodes ?? new ClassificationCodes();
        }
        // If we get a 404 we try to get the codes in the fallback language (nb)
        else if (response.StatusCode == HttpStatusCode.NotFound && language != "nb")
        {
            string fallbackQuery = BuildQuery("language=nb", selectDate, selectLevel, selectVariant, selectedCodes, selectedTargetClassificationId);
            var fallbackResponse = await _httpClient.GetAsync($"{url}{fallbackQuery}");
            if (fallbackResponse.IsSuccessStatusCode)
            {
                var fallbackResponseJosn = await fallbackResponse.Content.ReadAsStringAsync();
                if (!selectedTargetClassificationId.IsNullOrEmpty())
                {
                    var corrClassificationCodes=BuildClassificationCodes(fallbackResponseJosn,concateChildren);
                    return corrClassificationCodes ?? new ClassificationCodes();    
                }

                var fallbackClassificationCodes = JsonSerializer.Deserialize<ClassificationCodes>(fallbackResponseJosn);
                return fallbackClassificationCodes ?? new ClassificationCodes();
            }
        }

        return new ClassificationCodes();
    }

    private static string BuildQuery(string selectLanguage, string selectDate, string selectLevel, string selectVariant, string selectCodes, string selectedTargetClassificationId)
    {
        return $"?{selectLanguage}{selectDate}{selectLevel}{selectVariant}{selectCodes}{selectedTargetClassificationId}";
    }

    private static ClassificationCodes BuildClassificationCodes(string responseJson, string concateChildren = "")
    {
        ClassificationCodes corrClassificationCodes=new();
        var correspondenceCodes = JsonSerializer.Deserialize<CorrespondenceClassificationCodes>(responseJson);
        if (correspondenceCodes!=null)
        {
            if (concateChildren.IsNullOrEmpty())
            {
                corrClassificationCodes.Codes =correspondenceCodes.CorrespondenceItems.Select(x => new ClassificationCode(string.Concat(x.SourceCode,"#",x.TargetCode), x.SourceName, "1"){ParentCode=x.SourceCode,ShortName=x.SourceShortName,Notes = x.TargetName}).ToList();
            }
            else
            {
                if (concateChildren=="1") corrClassificationCodes.Codes =correspondenceCodes.CorrespondenceItems.Select(x => new ClassificationCode(x.SourceCode, x.SourceName, "1"){ParentCode=x.SourceCode,ShortName=x.SourceShortName, Notes = string.Join(", ", correspondenceCodes.CorrespondenceItems.Where(p => p.SourceCode == x.SourceCode).Select(u => u.TargetName))}).DistinctBy(w => w.Code).ToList();
                if (concateChildren=="2") corrClassificationCodes.Codes =correspondenceCodes.CorrespondenceItems.Select(x => new ClassificationCode(x.SourceCode, string.Concat(x.SourceName," (",string.Join(", ", correspondenceCodes.CorrespondenceItems.Where(p => p.SourceCode == x.SourceCode).Select(u => u.TargetName)),")"), "1"){ParentCode=x.SourceCode,ShortName=x.SourceShortName}).DistinctBy(w => w.Code).ToList();
            }

            return corrClassificationCodes ?? new ClassificationCodes();    
        }
        return new ClassificationCodes();
    }
}
