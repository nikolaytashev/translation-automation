namespace Translation.Automation.Core;

public interface ITranslateService
{
    ValueTask<IEnumerable<FileTranslationResult>> TranslateJsonFile(string filePath, Language sourceLanguage, IEnumerable<Language>? targetLanguages);

    ValueTask<FileTranslationResult> TranslateJsonFile(string filePath, Language sourceLanguage, Language targetLanguage);
}