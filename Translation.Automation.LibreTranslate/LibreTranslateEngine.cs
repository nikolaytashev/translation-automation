using System.Text.Json;
using Translation.Automation.Core;

namespace Translation.Automation.LibreTranslate;

public class LibreTranslateEngine : ITranslateEngine, IDisposable
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri("https://libretranslate.com"),
    };

    private IDictionary<Language, string>? _languages;

    public LibreTranslateEngine()
    {
        // needed to bypass the api key
        _client.DefaultRequestHeaders.Add("Origin", "https://libretranslate.com");
        _client.DefaultRequestHeaders.Add("Referer", "https://libretranslate.com/docs/");
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.81 Mobile Safari/537.36");
    }

    public async ValueTask<TranslationResult> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage)
    {
        var source = await GetIsoLanguageAsync(sourceLanguage);
        var target = await GetIsoLanguageAsync(targetLanguage);
        using var content = new FormUrlEncodedContent(new []
        {
            new KeyValuePair<string, string>("q", text),
            new KeyValuePair<string, string>("source", source),
            new KeyValuePair<string, string>("target", target),
            new KeyValuePair<string, string>("format", "text"),
            new KeyValuePair<string, string>("api_key", "")
        });
        using var response = await _client.PostAsync("/translate", content);
        var result = JsonSerializer.Deserialize<LibreTranslate>(await response.Content.ReadAsStringAsync(), _jsonSerializerOptions);
        if (result == null)
            throw new InvalidOperationException("Unable to parse translate result");

        return new TranslationResult(result.TranslatedText, sourceLanguage, targetLanguage);
    }

    public async ValueTask<IEnumerable<TranslationResult>> TranslateAsync(IEnumerable<string> texts, Language sourceLanguage, Language targetLanguage)
    {
        var tasks = texts.Select( async x => await TranslateAsync(x, sourceLanguage, targetLanguage));
        return await Task.WhenAll(tasks);
    }

    public ValueTask<IDictionary<string, object>> TranslateNestedDictionaryAsync(IDictionary<string, object> data, Language sourceLanguage, Language targetLanguage)
    {
        throw new NotImplementedException();
    }

    private async ValueTask<string> GetIsoLanguageAsync(Language language)
    {
        _languages ??= await RetrieveLanguages();
        if (!_languages.TryGetValue(language, out var value))
            throw new InvalidOperationException($"{language.GetName()} is not supported language");
        
        return value;
    }

    private async ValueTask<IDictionary<Language, string>> RetrieveLanguages()
    {
        var response = await _client.GetStringAsync("/languages");
        var result = JsonSerializer.Deserialize<List<LibreLanguage>>(response, _jsonSerializerOptions);
        if (result == null)
            throw new InvalidOperationException("Unable to parse languages result");
        
        var s = result.Select(x =>
            {
                var success = Enum.TryParse<Language>(x.Name, true, out var language);
                return (success, language, x.Code);
            })
            .Where(x => x.success)
            .ToDictionary(x => x.language, x => x.Code);

        return s;
    }

    public void Dispose() => _client.Dispose();
}