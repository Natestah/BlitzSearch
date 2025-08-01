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

![2025-07-24_10-36-36](https://github.com/user-attachments/assets/1d4363ab-1b4f-496c-a25c-52e78aa1e530)


## Setup Instructions

1) Install Blitz Search From Current Releases here. 
2) Install the Extension "Blitz Search" from your IDE's marketplace. 
3) (Optional) Bind blits* commands to a key. 
    * farther instructions are typically displayed on the marketplace extension information pages.

## More Info:

[Blitz Search Features on Youtube](https://youtube.com/playlist?list=PLDB5sR-xyaUYymdLPoywoApQ1ZlLl157d&si=6hpIiOI5kr7kPH8k) - Demonstrations of features

[Blitz Discord](https://discord.com/invite/UYPwQY9ngm) - Feedback is welcome, This is my personal Discord and shares topics of game dev things.

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

## Aspirations

Blitz Search is my baby, but I need a partner in this. 

I have a lot of ideas about what the future holds for Blitz Search.  If you find yourself aligning with the heart of Blitz Search and want to start making contributions.  Here are some things that I have been mulling.

1) Cross platform (currently only windows). I had thought to start with Linux, from there a Mac Port should be pretty straight forward.

2) Always Supporting New IDE's, Amazon Just put out a new VSCode derived one that should be easy (see my cursor or Windsurf commits).  You can follow any of the commits for the other IDE's to see how those are done.

3) Fuzzy Search, This looks more like "Did you mean?" in Google where there's a clear explanation about only missed words.  Blitz Search can respond in an instant Since those words are stored in a unified cache per extension, ( we already know the words that aren't going to find anything )
    * There are libraries that make this easy.

4) Highlighting in search box, This entails changing it from a TextBox, too a full-blown AvaloniaEdit box, with a custom highlighter.
    * Highlight things like the query symbols
    * Indicate inline the words that missed.

5) Search box context menu.
    * Search Box should show hints for the Query when the cursor is on a word IE: Change this word to a not (!word), or Force Case Sensitive ^casesensitve, or Whole word @wholeword

6) Word Completion, again with blitz search already having a complete view of all the words (when its fully cached), it can tell about words

7) CLI version,  Get a basic command line version going where we simply provide the results and work with the cache of a file.  

8) Open source Blitz.Query, this is probably the biggest takeaway from this tool, for things like Jetbrains, all that's needed is a customized matching and the inline version suddenly becomes better than Blitz Search.

9) Visit all Extensions and add "Show Blitz Search" commands. It's something that I miss, and it is really easy to add. (It's like Search This, but without populating the box..)

10) Improve Dirty Text handling,  One thing that IDE's can do with their search is find/replace within working text buffers that haven't been written to disk.  This requires going out to the Plugins and doing some work there and then some more work to communicate the buffer's.

11) Anything to do with hands off the keyboard
