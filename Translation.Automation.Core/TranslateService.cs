using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Translation.Automation.Core;

public class TranslateService : ITranslateService
{
    private readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    private readonly ITranslateEngine _translateEngine;

    public TranslateService(ITranslateEngine translateEngine)
    {
        _translateEngine = translateEngine;
    }

    public async ValueTask<IEnumerable<FileTranslationResult>> TranslateJsonFile(string filePath, Language sourceLanguage, IEnumerable<Language>? targetLanguages)
    {
        var languages = (targetLanguages ?? Enum.GetValues<Language>())
            .Where(x => x != sourceLanguage);

        var results = new List<FileTranslationResult>();
        foreach (var language in languages)
        {
            try
            {
                results.Add(await TranslateJsonFile(filePath, sourceLanguage, language));
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured while translating file to {language.GetName()}");
                Console.WriteLine(e);
            }
        }

        return results;
    }
    
    public async ValueTask<FileTranslationResult> TranslateJsonFile(string filePath, Language sourceLanguage, Language targetLanguage)
    {
        if (sourceLanguage == targetLanguage)
            return new FileTranslationResult(filePath, targetLanguage);
        
        var extension = Path.GetExtension(filePath)[1..];
        if (extension != "json")
            throw new InvalidOperationException($"File {filePath} is not a json file.");
        
        var text = await File.ReadAllTextAsync(filePath);
        var data = Deserialize(text);
        
        var resultData = await _translateEngine.TranslateNestedDictionaryAsync(data, sourceLanguage, targetLanguage);
        
        var resultText = JsonSerializer.Serialize(resultData, _options);
        var resultFilePath = $"{targetLanguage.GetLanguageCode()}.{extension}";
        await File.WriteAllTextAsync(resultFilePath, resultText);
        
        return new FileTranslationResult(resultFilePath, targetLanguage);
    }

    private IDictionary<string, object> Deserialize(string text)
    {
        var data = JsonSerializer.Deserialize<IDictionary<string, object>>(text);
        if (data == null)
            return new Dictionary<string, object>();
        
        foreach (var (key, value) in data)
        {
            if (value is not JsonElement element)
                continue;
            
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    data[key] = Deserialize(element.GetRawText());
                    break;
                
                case JsonValueKind.String:
                    data[key] = element.GetString() ?? string.Empty;
                    break;
                
                /*case JsonValueKind.Array:
                    data[key] = (element.Deserialize<object[]>() ?? Array.Empty<object>())
                        .Select(x => Deserialize(x))
                    break;*/
            }
        }

        return data;
    }

    /*private async ValueTask<IDictionary<string, object>> TranslateDataAsync(IDictionary<string, object> data,
        Language sourceLanguage, Language targetLanguage)
    {
        foreach (var (key, value) in data)
        {
            data[key] = value switch
            {
                string s => (await _translateEngine.TranslateAsync(s, sourceLanguage, targetLanguage)).TranslatedText,
                IDictionary<string, string> d => await _translateEngine.TranslateAsync(d.Values, sourceLanguage, targetLanguage),
                IDictionary<string, object> d => await TranslateDataAsync(d, sourceLanguage, targetLanguage),
                _ => data[key]
            };
        }

        return data;
    }*/
}