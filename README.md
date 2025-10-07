
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/natestah/blitzsearch/total?label=Installs)
![Sublime](https://img.shields.io/packagecontrol/dt/BlitzSearch?label=Sublime%20Text)
![Visual Studio Marketplace Downloads](https://img.shields.io/visual-studio-marketplace/d/NathanSilvers.BlitzSearch?label=VS%20Code)
![Visual Studio Marketplace Downloads](https://img.shields.io/visual-studio-marketplace/d/NathanSilvers.BlitzSearchVS?label=Visual%20Studio)
![JetBrains Plugin Downloads](https://img.shields.io/jetbrains/plugin/d/24564-blitzsearch?label=Jetbrains%20)


# Blitz Search 

A Universal improvement to Find/Replace in files for any Text Editor / IDE

<img width="1920" height="1099" alt="blitzthing14" src="https://github.com/user-attachments/assets/b238dd0a-cc19-4463-b323-1435a84d9181" />



## Bullets

* Blitz Query - "Word's on a line"
* Real time results
  * ♻️ Results Recycling / Caching help to reduces wear on IO
* Robust Find and Replace
* Handles Large volumes of files. 
* Syntax Highlighting
* Optional Git Ignore filter
  * optional .blitzIgnore files too.
* Auto-Human text file filtering.
* Living search results reflect file changes.
* In editor preview pane ( where applicable )
* Built in Folder Scope Definitions
* Automatic Updates, ( Update Button that shows Version changes on Mouse hover )

![2025-07-24_10-36-36](https://github.com/user-attachments/assets/1d4363ab-1b4f-496c-a25c-52e78aa1e530)


## Setup Instructions

1) Install Blitz Search From Current Releases here. 
2) Install the Extension "Blitz Search" from your IDE's marketplace. 
3) (Optional) Bind blits* commands to a key. 
    * farther instructions are typically displayed on the marketplace extension information pages.

## YouTube Demonstrations:

[Blitz Search Features on Youtube](https://youtube.com/playlist?list=PLDB5sR-xyaUYymdLPoywoApQ1ZlLl157d&si=6hpIiOI5kr7kPH8k) - Demonstrations of features, some of these are out of date but the functionality still exists.

## Supported IDEs and Extension Source

There are varying levels of support on these, with all but Jetbrains having feature parity.  You should be able to rebind Find/Replace in these editors.

I personally have Blitz Search as the stand in for all but Jetbrains ( there are some points there that I wont ever be able to hit ).

* [Sublime Text](https://github.com/Natestah/BlitzSt)
* [Visual Studio Code / Cursor / Windsurf ](https://github.com/Natestah/blitzSearchVSCode)
* [Notepad++](https://github.com/Natestah/BlitsNppPlugin)
* [Visual Studio](https://github.com/Natestah/BlitzVisualStudioExtension) 
* [IntelliJ - Jetbrains IDEs](https://github.com/Natestah/BlitzIntellij)
* [NeoVim](https://github.com/Natestah/blitzsearch.nvim)

## Dependencies
* [AvaloniaUI](https://github.com/avaloniaui/) - Modern Presentation framework
* [AvaloniaEdit](https://github.com/avaloniaui/avaloniaedit) - Text Editor
* [TextMateSharp](https://github.com/AvaloniaUI/AvaloniaEdit/) - Syntax Highlighting
* [Huminizer](https://github.com/Humanizr/Humanizer) - Making Timestamps and file sizes more humanly readable.
* [Material.Icons](https://github.com/SKProCH/Material.Icons) - Material Icons provides much of the icons used throughout.

## Special thanks

Super early testing and feedback:
* Stephen McCarty
* Carlos Pineda
* Robert Dye
* Noé Pfister
* Herr Kaste

Killer Blitz Icon:
* Oscar Lopez

# The Future of Blitz Search

I NEED CONTRIBUTORS to help realize the vision of Blitz Search!  

This vision is a central place for simple text search, While IDE's focus on LSP, and now AI agent development. Find-in-File promises to be even farther left behind.

I have worked hard to reach parity with the best of the best Find-in-Files, now it's time to take it to the next level!

If you find yourself aligning with the heart of Blitz Search and want to start making contributions.  

Here is as much as I could think of to layout as a roadmap.  Eventually, likely sooner, I'm going to take an indefinite break from all this.

### Reaching People

1) Cross platform (currently only windows). I had thought to start with Linux, from there a Mac Port should be pretty straight forward.
   * There's some code relating to a window style ( putting things on the task bar in Windows ), where custom Maximize Close buttons need to mimic default MAC OS buttons
   * gotodefs.json needs a platform version, this describes default install locations for applications
   * I have habitually used Windows Path Separators,  This should be easy to fix up ( use Path.Combine )

2) Always Supporting New IDE's, You can follow any of the commits for the other IDE's to see how those are done.
   * Amazon's new IDE is VSCODE, should be easy! 
   * Obsidian
   * Going to lesser known IDE's, is a great way to find passionate developers willing to chime in on feedback.  Sublime text has only 38 downloads and those guys have said a lot!

3) CLI Version,  There maybe some IDE's out there that can work on a CLI version.  Might be a good path to take to try and recognize the same parameters set by RIPGREP and things (if that makes sense)
    * This isn't a huge breakout.  New Command Line project + Blitz.Search.csproj.  Works Without GUI

## Improving Search, Increasing value

1) Fuzzy Search, This looks more like "Did you mean?" in Google where there's a clear explanation about only missed words.  
   * Blitz Search can respond in an instant Since those words are stored in a unified cache per extension, ( we already know the words that aren't going to find anything ) 
   * There are libraries that make this easy.

2) Highlighting in search box, This entails changing it from a TextBox, too a full-blown AvaloniaEdit box, with a custom highlighter.
    * Highlight things like the query symbols
    * Indicate inline the words that missed.

3) Search box context menu.
    * Search Box should show hints for the Query when the cursor is on a word IE: Change this word to a not (!word), or Force Case Sensitive ^casesensitve, or Whole word @wholeword

4) Word Completion, again with blitz search already having a complete view of all the words (when its fully cached), it can tell about words

5) Open source Blitz.Query, this is probably the biggest takeaway from this tool, for things like Jetbrains, all that's needed is a customized matching and the inline version suddenly becomes better than Blitz Search.

6) Visit all Extensions and add "Show Blitz Search" commands. It's something that I miss, and it is really easy to add. (It's like Search This, but without populating the box..)

7) Improve Dirty Text handling,  One thing that IDE's can do with their search is find/replace within working text buffers that haven't been written to disk.  This requires going out to the Plugins and doing some work there and then some more work to communicate the buffer's.

8) Anything to do with hands off the keyboard, I think about maybe being inspired by VIM,  Hotkey to focus results/Switch Scopes. I was hoping ot reveal a bit of this from NVIM users.

9) AI Features - Ability to Speak to an AI about results, or DO Large Language things 
   * https://github.com/Zelex/aisearch - Check this out! Jon's example to me was search for "//Todo: " then ask AI to "Tell me about the todo's" 
   * AI could be used to derive a complex OR operator search internally to soften the results or otherwise leverage LLM, I have considered using an English thesaurus, but LLM would be even cooler as it might yield cross language improvments.
     * Using Thesaurus for exakmple here: "execute" would translate to an or search ( perform|accomplish|fulfil|achieve|do|implement|make )

10) Multi-media, I Jettisoned this Idea before, but I thought it could be cool to show the image when a filename matched. a search for ".png" with filenames would yield a nice presentation of images along with text results

## Code Quality

1) Compiler complaints about Nullables hit me hard in this project, and I really didn't have a good answer to when a non-nullable reference found a way to become null.
   * I can't remember if this was from JSON serialization or from MessagePack.  Would be nice to have some tests, that replicate what I suspect, and then maybe some code generators to  do what is appropriate.  I would like to have Collections be non-nullable and reliably enumerated.  Perhaps solutions in either exists already.
2)  Warnings as Errors
   * I have abandoned this at some point, most likey due to the above.
3) Blitz Query
   * I tacked on Literal/Regular Expressions to search, I would like for those to simply be SubQueries of the root "BlitzAddQuery" type, this would clean up quite a bit of code and noise there.
4) Unit Tests
   * Any retroactive Unit Tests would be helpful in maintaining stability  

## Github

1) I would like to develop a bit more Deploy triggers.  
    * Currently Deploy offers nothing in Description, lets fix this up.

## Social Engagement

1) This has been at the heart of Blitz Search from the start, I need a vocal hero to elevate Blitz Search,  Every new feature is an opportunity to blast social about it.
   * It would be cool to have brand new channels Dedicated, I have destroyed my profiles algorithms by talking about Game Development stuff.  
