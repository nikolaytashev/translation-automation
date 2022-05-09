// See https://aka.ms/new-console-template for more information

using Translation.Automation.Core;
using Translation.Automation.GoogleTranslateSelenium;
using System.CommandLine;
using System.CommandLine.Binding;

var engine = new SeleniumTranslateEngine();
ITranslateService translateService = new TranslateService(engine);

var singleWordCommand = new Command("text", "Translate text")
{
    new Argument<string>("text", "Text to translate"),
    new Argument<Language>("source-language", "The source language of the text"),
    new Argument<Language>("target-language", "The target language for the translation"),
};
singleWordCommand.SetHandler(async (string text, Language sourceLanguage, Language targetLanguage, IConsole console) =>
{
    var result = await engine.TranslateAsync(text, sourceLanguage, targetLanguage);
    console.WriteLine("Translated result:");
    console.WriteLine(result.TranslatedText);
}, singleWordCommand.Arguments.Cast<IValueDescriptor>().ToArray());

var jsonFileCommand = new Command("json", "Translate a json file")
{
    new Argument<FileInfo>("filePath", "Path to the json file").ExistingOnly(),
    new Argument<Language>("source-language", "The source language of the file"),
    new Argument<Language[]>("target-languages", Array.Empty<Language>, 
        "The target languages for the translation. If the argument is not provided - all languages will be translated"),
};
jsonFileCommand.SetHandler(async (FileInfo filePath, Language sourceLanguage, Language[]? targetLanguages, IConsole console) =>
{
    targetLanguages = targetLanguages!.Any() ? targetLanguages : null;
    var fileResults = await translateService.TranslateJsonFile(filePath.FullName, sourceLanguage, targetLanguages);
    foreach (var fileResult in fileResults)
        console.WriteLine(
            $"File {fileResult.FilePath} saved with target language {fileResult.TargetLanguage.GetName()}");
}, jsonFileCommand.Arguments.Cast<IValueDescriptor>().Concat(jsonFileCommand.Options).ToArray());

var rootCommand = new RootCommand();
rootCommand.AddCommand(singleWordCommand);
rootCommand.AddCommand(jsonFileCommand);

await rootCommand.InvokeAsync(args);
