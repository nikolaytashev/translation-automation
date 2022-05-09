using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Translation.Automation.Core;

namespace Translation.Automation.GoogleTranslateSelenium;

public class SeleniumTranslateEngine : ITranslateEngine
{
    private readonly TimeSpan _defaultTimeOut = TimeSpan.FromSeconds(15);
    
    public async ValueTask<TranslationResult> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage)
    {
        Console.WriteLine($"Translating '{text}' from {sourceLanguage.GetName()} to {targetLanguage.GetName()}");
        using var webDriver = await InitDriverAsync(sourceLanguage, targetLanguage);
        
        var translation = TranslateAsync(text, webDriver, sourceLanguage, targetLanguage);
        var result = new TranslationResult(translation, sourceLanguage, targetLanguage);
        
        webDriver.Quit();
        
        return result;
    }
    
    public async ValueTask<IEnumerable<TranslationResult>> TranslateAsync(IEnumerable<string> texts, Language sourceLanguage, Language targetLanguage)
    {
        using var webDriver = await InitDriverAsync(sourceLanguage, targetLanguage);

        var result = texts
            .Select(text => TranslateAsync(text, webDriver, sourceLanguage, targetLanguage))
            .Select(translation => new TranslationResult(translation, sourceLanguage, targetLanguage))
            .ToList();

        webDriver.Quit();
        
        return result;
    }

    public async ValueTask<IDictionary<string, object>> TranslateNestedDictionaryAsync(IDictionary<string, object> data,
        Language sourceLanguage, Language targetLanguage)
    {
        using var webDriver = await InitDriverAsync(sourceLanguage, targetLanguage);
        
        data = await TranslatedDictionaryAsync(data, webDriver, sourceLanguage, targetLanguage);
        
        webDriver.Quit();

        return data;
    }
    
    private async ValueTask<ChromeDriver> InitDriverAsync(Language sourceLanguage, Language targetLanguage)
    {
        var webDriver = new ChromeDriver();
        
        var elementWait = new WebDriverWait(webDriver, _defaultTimeOut);
        elementWait.IgnoreExceptionTypes(typeof(NoSuchElementException));

        webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
        webDriver.Navigate().GoToUrl("https://translate.google.com/");

        var gdprConsent = webDriver.FindElements(By.CssSelector(
            "button[aria-label='Agree to the use of cookies and other data for the purposes described']"));
        if (gdprConsent.Any())
            gdprConsent.First().Click();

        var sourceLangListButton = elementWait.Until(driver =>
            driver.FindElement(By.CssSelector("button[aria-label='More source languages']")));
        sourceLangListButton.Click();

        var sourceLanguageName = sourceLanguage.GetName();
        var sourceLanguageButton = new WebDriverWait(webDriver, _defaultTimeOut)
            .Until(driver =>
            {
                return driver
                    .FindElements(By.XPath($"//div[.//div[text()='{sourceLanguageName}']]"))
                    .FirstOrDefault(x =>
                        !string.IsNullOrEmpty(x.GetAttribute("data-language-code")) && x.Displayed);
            });

        sourceLanguageButton!.Click();

        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var targetLangListButton = elementWait.Until(driver =>
            driver.FindElement(By.CssSelector("button[aria-label='More target languages']")));
        targetLangListButton.Click();
        
        var targetLanguageName = targetLanguage.GetName();
        var targetLanguageButton = new WebDriverWait(webDriver, _defaultTimeOut)
            .Until(driver =>
            {
                return driver
                    .FindElements(By.XPath($"//div[.//div[text()='{targetLanguageName}']]"))
                    .FirstOrDefault(x =>
                        !string.IsNullOrEmpty(x.GetAttribute("data-language-code")) && x.Displayed);
            });
        targetLanguageButton!.Click();
        
        return webDriver;
    }

    private async ValueTask<IDictionary<string, object>> TranslatedDictionaryAsync(IDictionary<string, object> data,
        IWebDriver webDriver, Language sourceLanguage, Language targetLanguage)
    {
        foreach (var (key, value) in data)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            data[key] = value switch
            {
                string s => TranslateAsync(s, webDriver, sourceLanguage, targetLanguage),
                IDictionary<string, object> d => await TranslatedDictionaryAsync(d, webDriver, sourceLanguage, targetLanguage),
                _ => data[key]
            };
        }

        return data;
    }
    
    private string TranslateAsync(string text, IWebDriver webDriver, Language sourceLanguage, Language targetLanguage)
    {
        var elementWait = new WebDriverWait(webDriver, _defaultTimeOut);
        elementWait.IgnoreExceptionTypes(typeof(NoSuchElementException));
        
        try
        {
            Console.WriteLine($"Translating '{text}' from {sourceLanguage.GetName()} to {targetLanguage.GetName()}");
            
            var textArea = elementWait.Until(driver =>
                driver.FindElement(By.CssSelector("textarea[aria-label='Source text']")));
            textArea.SendKeys(Keys.Control + 'a');
            textArea.SendKeys(Keys.Delete);

            elementWait.Until(driver => GetTranslationContainer(driver) == null);
            
            textArea.SendKeys(text);

            string result;
            try
            {
                result = ExtractTranslationText();
            }
            catch (StaleElementReferenceException)
            {
                result = ExtractTranslationText();
            }

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("Could not find translation element");
            
            /*new WebDriverWait(webDriver, _defaultTimeOut)
                .Until(_ => !string.IsNullOrEmpty(targetTextSpan.Text));*/
        
            Console.WriteLine($"Translated '{text}' to '{result}' from {sourceLanguage.GetName()} to {targetLanguage.GetName()}");
            return result;
        }
        catch (Exception)
        {
            Console.WriteLine($"An error occured translating '{text}' from {sourceLanguage.GetName()} to {targetLanguage.GetName()}");
            throw;
        }
        
        string ExtractTranslationText()
        {
            var translatedTextElement = elementWait.Until(GetTranslationContainer);
            //var targetTextSpan = translatedTextElement!.FindElement(By.TagName("span"));
            
            return translatedTextElement!.GetAttribute("data-text");
        }

        IWebElement? GetTranslationContainer(IWebDriver driver)
        {
            /*var result = driver.FindElements(By.CssSelector("span[data-language-for-alternatives]"))
                .FirstOrDefault();
            
            if (result != null)
                return result;
            
            result = driver.FindElements(By.CssSelector("div[data-result-index='0']"))
                .FirstOrDefault();
            result = result?.FindElement(By.TagName("div"));
            
            return result;*/
            var result = driver.FindElements(By.XPath("//div[string-length(@data-text)>0]"))
                .FirstOrDefault();

            return result;
        }
    }
}