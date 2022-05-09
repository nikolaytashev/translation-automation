# Translation.Automation
Application for translating text and json files

**Prerequisites:** .NET 6

# Build
`dotnet build -c Release`

# Usage
### Available Languages
- English,
- Japanese,
- Portuguese,
- French,
- ChineseSimplified,
- German,
- Russian,
- Spanish,
- Vietnamese,
- Thai

### Available commands
 - text - translate text  
`Translation.Automation.Application text {Your text} {Source language} {Destination language}`  
example:  
`'Translation.Automation.Application text Hello English Spanish`


 - json - translate json file  
   `Translation.Automation.Application json {JSON file path} {Source language} Optional:[{Destination language}]`   
If no arguments are provided for the Destination language - the json file will be translated to all available languages
Otherwise - the it will be translated to the provided languages.
example:  
   `Translation.Automation.Application json en.json English`  
   `Translation.Automation.Application json en.json English [Spanish Thai]`