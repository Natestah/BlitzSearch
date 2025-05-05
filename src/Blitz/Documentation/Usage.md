## Blitz finds words on a line

Consider the Following Line:

>Blitz Search works by matching "**WORDS** on a line".  It also matches **FileNames**.

"words filenames" will match the line, order does not matter. Think of Spaces as an AND operator.

## Advanced Queries

For most searches simple words are enough, but there may come results that can clutter the view. Consider the following lines:
>Apples taste good
>
>Oranges are good

### OR |

"**apples|oranges**" will have Both lines in your results

### NOT !

"good **!apples**"  will exclude the line containing apples


### Whole Word @

"@apple" matches nothing"
"@apples" matches the line containing the whole word

### NOT ! and Whole words @ can be used at the end of the word.

"good **apples!**" will exclude the line containing apples
"apples@" matches the line containing the whole word

## Case Sensitivity 

Case sensitivity works through smart casing. Which is, a word with any uppercase letters is sensitive.

Consider:

>Apples
> 
>apples

"apples" matches "apples" AND "Apples"

"Apples" matches "Apples" only

place a ^ before the word to enforce case sensitivity on the word ( to match only lower case )

^apples matches "apples" only.


## Blitz Is Not about fuzziness or AI

Blitz is not a heuristic or AI driven! Fuzzy search and AI is difficult to get right and In my own humble option, a slippery slope. 

I hope that Blitz Search can appeal to Humans wanting to look at code without some AI interpretation of intents.

## Plugin Setup Guides:
Many IDE's and Editors feature plugins that allow you to set the search field in Blitz Without Using Copy-Paste.

#### [NotePad++](https://natestah.com/blog/f/blitz-search-and-notepad)
#### [VS Code](https://natestah.com/blog/f/blitz-search-with-vs-code)
#### [Visual Studio](https://natestah.com/blog/f/blitz-search-with-visual-studio)
#### [Jetbrains](https://natestah.com/blog/f/blitz-with-jetbrains-ides)
