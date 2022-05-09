namespace Translation.Automation.Core;

public static class Extensions
{
    public static string GetName(this Language language)
    {
        return language switch
        {
            Language.ChineseSimplified => "Chinese (Simplified)",
            _ => Enum.GetName(language)!
        };
    }

    public static string GetLanguageCode(this Language language)
    {
        return language switch
        {
            Language.English => "en",
            Language.Japanese => "ja",
            Language.Portuguese => "pt",
            Language.French => "fr",
            Language.ChineseSimplified => "zh",
            Language.German => "de",
            Language.Russian => "ru",
            Language.Spanish => "es",
            Language.Vietnamese => "vi",
            Language.Thai => "th",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }
}