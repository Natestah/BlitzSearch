# Welcome  to Blitz Search

The official Deployment of Blitz Search is at https://natestah.com - where the executable has been Signed by my Company, Natestah LLC And has an Updater included.

![Blitz Search Sample](https://blitzsearch.s3.us-east-2.amazonaws.com/BlitzPretty2x.png)

![Blitz128](https://github.com/Natestah/BlitzSearch/assets/11800697/dcd68d7f-da5c-4dae-bf8e-02b88e37cfa9)

---
# What is Blitz Search?

Mirrored from https://natestah.com

## Universal Search
* A Stand alone Windows (x64) Search Tool that works together with Many Popular IDEs and Text Editors.
## Dynamic Results, its not just about Speed
* Search code for words on a line.
* Stack query types ( words, and literal, or regex )
* Real time results update while you type.
* Replace words or Literal or Regular expression.
* Handles Large volumes of files quickly.
* Syntax Highlighting in results,  highlights on matches are easy on the eyes.
* Git Ignore file filter reduces workload and Clutter in results.
* Auto-Human text file discovery Takes burden off Filtering ( Don't worry about heavy binary files getting in the way )
* Quality of Life focused.
## Companion for Popular Editors and IDEs
* Can open editors directly to the results, Acting as a stand-in for Control-Shift-F  and Control-Shift-H ( replace in files )
* Plugins available for Visual Studio Code, Visual Studio, All JetBrains IDEs, Sublime Text, and Notepad++
* Plugins Communicate with Blitz Search to Find/Replace the word at the cursor, much the same as selecting the word and pressing Control-Shift-F/H

# Samples:


### Set multiple sets of multiple paths and associate an icon for easy switching 

![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/ConfigurePaths.png)

### Full Text Preview ( AvaloniaEdit! )
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/FullTextPreview.png)

### Regular Expression Search is awesome to see in Real Time!
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/RegularExpression.png)

### Replace with Over / Under style
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/ReplaceText.png)

### Context Menu lets you Goto Any Text Editor!
### See: ( https://github.com/Natestah/Blitz.GotoEditor )
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/RightClickOptions.png)

### Supports traditional Literal / Regular expression searches too!
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/TraditionalLiteralSearch.png)

### Theme support
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/UsePopularThemes.png)

### Words in any order
![sample](https://blitzsearch.s3.us-east-2.amazonaws.com/WordsInAnyOrder.png)



## Code details:

This is the Main application for Blitz Search. 

Pardon the mess, the repository has been private until now so I've been a little bit undisciplined 

Blitz.csproj - Main Avalonia Application Houses Documentation, Version Checking, Global Exception handling ( print to local HTML before crashing )
Blitz.Configuration - These are Json Configuration options, JsonContext provides setup for NativeAOC
Blitz.Avalonia.Controls - Houses everything inside the window.  It's a bit of a mess and I have some todo's here but this is where the wheels hit the road.  User control templates and behaviors

## More Info:

[Blitz Search Features on Youtube](https://youtube.com/playlist?list=PLDB5sR-xyaUYymdLPoywoApQ1ZlLl157d&si=6hpIiOI5kr7kPH8k) - Demonstrations of features

[Natestah.com](https://natestah.com/) - Main landing page for Blitz Search

[LinkedIn Nathan Silvers](www.linkedin.com/in/nathan-silvers-a17308a8) - Personal LinkedIn Profile

[Blitz Discord](https://discord.com/invite/UYPwQY9ngm) - new builds posted here first.. Join the conversation

