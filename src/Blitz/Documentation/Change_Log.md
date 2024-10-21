# Change Log

# 🎉Blitz Search now  Free🎉

Please support Blitz Search by engaging social media's.

#### Come join me on.. [Discord Preferred](https://discord.com/invite/UYPwQY9ngm) or [Reddit](https://www.reddit.com/r/BlitzSearch) and give feedback!

Thank you!
### Version 0.0.72
* Removing Premium checks, Blitz Search Is fully free
* Small banner has been added, subject to some more change. I ask that you click this and watch and engage. Help me get the word out..
* I am checkpointing my work on the built-in text editor. There is a new file tab, with preview mode. More on this later. 
### Version 0.0.70
* Adding 30 day Trial for Premium Blitz
* Ignore .TMP files ( for Visual Studio )
### Version 0.0.69
* Update to handle Replace This Command from VSCode
### Version 0.0.68
* Changing .gitIgnore setting on Scope settings now updates the results. 
### Version 0.0.67
* Global Case sensitivity selection is removed, read on!
* FileName search is always case-insensitive since windows filesystem is insensitive
* Added ^ operator to query to get per word granularity on Case sensitivity
* Special operators can be Combined now (For whole word Case sensitive use: @^word )
* Special operators can be post-fixed now too. word@ is same as @word.  Since you might decide to append these after you've typed and seen the results need continued filtering.
* Humanized number of files, ( 10.3K instead of 10300 )
* Work to Improve responsiveness of Results when nothing is found for a time ( the old results clear out ).  Blitz search keeps old results for a time in order to prevent flickering.
* more immediate response to broken Regular expressions in Regex Search
* [YouTube - Case sensitivity demo](https://youtu.be/2TElEqcqpRk)
### Version 0.0.66
* Revamped Highlights, handles overlaps better.
* This paves way for more robust painting of highlights for future changes but also fixes the immediate problems in regex Literal replace.
### Version 0.0.65
* Fixed "Updated" results not updating correctly.
* Fixed up some Drawing issues with Various Find/Replace operations.
### Version 0.0.64
* Fixed Links to IDE setup in readme area.
* PREMIUM: Added selector box for Literal/Regular expression on Find/Replace
* Some work to Highlighter to better support mismatch in lengths with replacements. 
### Version 0.0.63
* Removed weird inline Left/Right Arrows in favor of "Space Bar" to toggle the text preview. This is clued by the right click menu and also paves way for folding results. 
* Add Toggle for Status bar in the Theme settings.
* Git ignore setting is moved from Settings to Per Scope setting.
* Decrease Hover time from 0.5 to 0.0 for ScrollBars
* Reduced Verbiage in Scope Settings.
### Version 0.0.62  
* Add Handler for Regular Expression Error in .gitIgnore.
### Version 0.0.61
* Fix RegexEx ShortCut Hint.
* Fixed bug in results in Literal search after an invalid normal search.
* Cleanup Goto Settings, squished selection, etc.~~~~
* Documentation work
* Fixed crash when clicking about on the very front page ( yikes )
* Some word-reduction on Settings page
### Version 0.0.60
* Blitz Search Premium sales is now live, check website for early bird special!
* [issues/54](https://github.com/Natestah/BlitzSearch/issues/54) Fix negative margins on maximized window at startup.
### Version 0.0.59
* Little Cleanup to Title Bar Behaviors. Window will shrink a bit better.
* Add Margins for Fullscreen mode to fix things going offscreen
* Margin to Settings window
* Keyboard Down arrow from search box will goto the first result line, then filename if no results in files.
* Keyboard Enter from search will Go directly to the first item in the list.
* Goto Has expander for Customize, also a scroll viewer for vertically challenged windows.
* Goto Editors have more Notes applied, all the Jetbrains products have plugins, etc.
* Goto Editor has a drop down Duplicate to better represent what things are doing.
* Fixed an unhandled exception I found while mucking with Goto Editor settings.
### Version 0.0.58
* Fixed up filename matching with Literal and Regex Searches
* Fixed results recycling up after adding new features of literal and regex.
* Update Roadmap to reflect addition of Regex feature
### Version 0.0.57
* Enable Regular Expression search
* More work to messaging when failed expressions happen.
* Fix Last Scope Selection being off when restarting Blitz
### Version 0.0.56
* Some work internally for Regex ( nothing to show yet )
* Improved crash handling on deployed Blitz Search.  Now shows HTML page for more systemic unhandled Exceptions ( so that I can go after stability )
### Version 0.0.55
* Fixed Crash that happened as I was demonstrating updates on file changes.
* Two places where I read the file immediately were contending with no recourse/failure handling.
* This is extra important for VS Code users as that tends to hold onto files for various reasons ( LSP and other extensions might hold the file )
* [Premium] Adjustable icon scale, for me to take Zooming out of my social Media post. Less post processing makes video's a little more crisp
### Version 0.0.54
* Fix a bug that I noticed today about Updating Results as files are changed.
### Version 0.0.53
* [issues/49](https://github.com/Natestah/BlitzSearch/issues/49) - Smart case works with Literal Search.
* Smart Case Close button makes adjustment to Literal Search. 
* Ignore Case sensitivity when searching the filenames0
### Version 0.0.52
* [Premium] Adding Literal search option. 
* Fix bug in file changes while Blitz is not running. Needed an extra bit of validation.
* Fix some bugs in Replace History.
### Version 0.0.51
* [Premium] Adding Quick Scope feature
* Minor fix to Selection Vertical Height causing jitter to view
### Version 0.0.50
* clean install fix exceptions.
### Version 0.0.49
* Inno Installer upgrade in hopes of thwarting false positive on Windows Defender virus detection.
* Updates for Right arrow in search results pulling out Preview
* Collapsable title menu ( more work needed here, but wanted for vertical social recordings )
* Some initial refactoring work on "Quick Scoping".  Should come online soon. Nothing feature wise.
### Version 0.0.48
* [Premium] Find-Replace 
* Added RoadMap To Documentation
* Move Right Pane Tabs up to title bar.
### Version 0.0.47
* More reduced memory usage.
### Version 0.0.46
* Update Goto for Jetbrains products to also look in LOCALAPPDATA for executables. noticed when I finally updated to 2024 bins.
### Version 0.0.45
* Results speed increase.. my 4.1 GB text folder went from ~400MS to last result found to <200MS ( sans recycling ) 
* This is achieved by re-ordering word storage so that we work on the same pool of words.
* Some efforts towards fixing up Results Recycling sometimes getting a bad set. Evasive due to high frequency of moving parts.
### Version 0.0.44
* Fix default font.. not everyone has "Cascadia Code"
### Version 0.0.43
* Visuals improvements, PNG->SVG for better fidelity 
* Custom Title bar allows Theme color to affect space better ( and rendering SVG icon )
* **BugFix:** Launch Registration Application from Program Files instead of "CurrentDirectory" to assure it lands correctly.
* Theoretical fix on right click crash on results view.
* Work on Folder selection, adding History and AutoCompleteBox.
### Version 0.0.42
* **BugFix:**: color fix on ':' for filename path
* [issues/47](https://github.com/Natestah/BlitzSearch/issues/47) Ordering on priority extensions getting mixed up on Recycle results.
* Ignore files ending with ~ ( backup files )
### Version 0.0.41
* Fix command line preview not showing in Goto Selection
* Fixed up default for Fleet Goto behavior. 
### Version 0.0.40
* Fixed Visual Studio missing from right click to goto
### Version 0.0.39
* Public Facing Version update feature. Some live tests were required
### Version 0.0.38
* Testing..
### Version 0.0.36
* Updater, fetches information about updates and automatically downloads / presents a button about it.
* Clicking update runs installer in Super Silent mode.  It should be quick and painless to install. 
* Removed pdb's from deploy for big Installer size saving.
* Installer is now signed by Natestah LLC.. Meaning Windows Defender won't bother you with a scary prompt.
* My update process is a little bit more Automated
### Version 0.0.35
* Fixed changed file when search box has nothing resulting in results.
* Cleanup status bar a bit. ( smaller, less verbiage)
* Add sublime text to IDE setup instructions.
* Fix F8 ( search this from preview ) Now Focuses and highlights text in search box ( like the editors do ) 
* [Issue 46](https://github.com/Natestah/BlitzSearch/issues/46) With an empty search field, changed files were creating bogus results
* [Issue 7](https://github.com/Natestah/BlitzSearch/issues/7) Add an option to show total file time (instead of forcing it)
### Version 0.0.34
* **Premium:** Theme Panel upgrade, Select Syntax Highlighting mode from more that just light/dark radio buttons
* **Premium:** Some Control over Results Spacing
* **Premium:** Global Font Selector.
* Right Click on a word in the preview to "Search on Github" simply tells web browser to open that up as a github search.
### Version 0.0.33
* another cache fix as I realized that GetHashCode is indeterminate,  Due to recent improvements to Directory Enumerations this flew under the radar.
### Version 0.0.32
* Fixed old results coming in when changing the scope, now unique caches are created foreach combination of directories.
* Send the results immediately after quiet time has elapsed. 
* More Accurate time on "Last result found" when recycling is enabled. Was Conflict with "quiet time" logic. 
### 0.0.31
* Fix Whole word considering underscore as a boundary.
* Adding "IDE Setup" with links to Setup instructions for each.
### Version 0.0.30
* Add Usage section to Help page
* Fix Reddit link
* Shrank some help images
* Some notes about What Blitz Search Is Not ( AI Driven, Fuzzy Search )
* Added Catch around some new file Discovery 
### Version 0.0.29
* Added parallel to directory enumeration
### Version 0.0.28 
* **BugFix:** - preview pane now updates when files are saved.
* Added Context Menu to Preview Pane with 2 new Hotkey bound commands
  * Goto Selected Editor ( F12 )
  * Search This ( F8 )
### Version 0.0.27
* Fix highlighting in preview when starting up. A mix up between "Dark" and "DarkPlus" (preferred) was flying under the radar.
* Work on Cache Recall, Before the cached recall was only benefiting the first of extension in the list. 
### Version 0.0.26
* Added Change Log and improve help section, Can now show images and things
* [Github #32](https://github.com/Natestah/BlitzSearch/issues/31) Memory improvement for caches
  * Removing common Hexadecimal words
  * underscores removed from remembered names
* [Github #36](https://github.com/Natestah/BlitzSearch/issues/36) Control-F for find-in-files is now Control-Shift-F, so as not to conflict with the Preview panes Control-F
* [Github #38](https://github.com/Natestah/BlitzSearch/issues/38) Fix FileName being recalled, but not shown, creating very misleading non-results.
* Added History Fly-out-drop-downs for file and search fields.
### Version 0.0.25
* [Github #35](https://github.com/Natestah/BlitzSearch/issues/35) Added Case Sensitivity  
* [Github #35](https://github.com/Natestah/BlitzSearch/issues/35) Smart Case Option ( Any Upper case character indicates case sensitive )
* [Github #35](https://github.com/Natestah/BlitzSearch/issues/35) bumped Avalonia to Beta 11.1.0.beta2 for a change ( radio buttons on menus )
### Version 0.0.24
* Added all Jetbrains IDE's to the Goto box
* Shrank the Items since there are many now! 
### Version 0.0.23 
* New feature to Update Results when files change
### Version 0.0.22
* Added granular parsing on .gitIgnore.. Instead of failing the whole thing, fail and log the line.
### Version 0.0.21
* Fixed an issue that popped up in v20.
### Version 0.0.20
* Right click Open in Explorer Option
* Right click Open with Command Prompt option
* Minor work to "files not found" UI in results field.
* **Bugfix:** Fixed inconsistency of report on number of files with Results Recycling.
* **Bugfix:**Much Work to improve behavior of changed files and .blitzIgnore .gitignore files when files change.
* Fixed Icons not showing up in published Exe ( NativeAOC related ).
### Version 0.0.19
* Added Special thanks ***Carlos Pineda*** for feedback
* Quality of life changes: Right Click menu in the search results to open editor other than what you have selected.
* Close Button on File Name Filter
* **.blitzIgnore** (file) acts like **.gitIgnore**.. For when you want to ignore in blitz, but not GIt. ( I'm still trying to find the context for this )
### Version 0.0.18
* Blitz now has an adjustable robot file detection that can help identify and save memory Or simply sort those to the end of the search list for quicker Human results.
* The detection is simply "Greater than X MB" or "First Line Longer than X characters". both of which are adjustable
* Added Cache Clean up button.
* Additional work when making adjustments to the Scope to weed out old caches and get back some memory.
### Version 0.0.17
* Work to Restore window state when invoked from outside (make window come to front).
### Version 0.0.15
* Stuff for Visual Studio Extension ( which is live now in Preview )
### Version 0.0.14
* Option to show in taskbar.
### Version 0.0.13
* Minor light theme fixes
* visual studio code plugin update (v6) to run needs this in Blitz too.  Running the command will now launch blitz And Update the search field accordingly.
### Version 0.0.12
* Dark/Light Theme.
* Fix an out of memory issue.
* Remember the split pane setting on closing.
### Version 0.0.11
* Integrates Licence management.   ( I haven't activated the storefront as there is nothing to sell yet ).
* Adding TimeToLastResult / Total Time.
* Removed option to not retain filenames. It was a heavily ignored and suboptimal code path.
* Describe the "Debug Filename" option better.
* Little bit of work to [ALL files] with priorities.
### Version 0.0.9
* Project Rider Goto