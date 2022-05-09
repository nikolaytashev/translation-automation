namespace Translation.Automation.Core;

public interface ITranslateEngine
{
    /// <summary>
    /// Translate text
    /// </summary>
    /// <param name="text"> The text to translate </param>
    /// <param name="sourceLanguage"> The source language </param>
    /// <param name="targetLanguage"> The target language </param>
    ValueTask<TranslationResult> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage);

    /// <summary>
    /// Translate a collection of texts
    /// </summary>
    /// <param name="texts"> The collection to translate </param>
    /// <param name="sourceLanguage"> The source language </param>
    /// <param name="targetLanguage"> The target language </param>
    ValueTask<IEnumerable<TranslationResult>> TranslateAsync(IEnumerable<string> texts, Language sourceLanguage,
        Language targetLanguage);

    /// <summary>
    /// Translate a nested dictionary of texts
    /// </summary>
    /// <param name="data"> The dictionary to translate </param>
    /// <param name="sourceLanguage"> The source language </param>
    /// <param name="targetLanguage"> The target language </param>
    ValueTask<IDictionary<string, object>> TranslateNestedDictionaryAsync(IDictionary<string, object> data,
        Language sourceLanguage, Language targetLanguage);
}