using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Blitz.Interfacing.QueryProcessing;

namespace Blitz.Avalonia.Controls;

public class MatchHighlighter(CharState?[] states, List<BlitzMatch> matches, string lineText, bool isForReplacement)
{
    public InlineCollection GetInlines()
    {
        InlineCollection baseCollection = new InlineCollection();
        var currentCollection = baseCollection;
        if (states.Length == 0)
        {
            return baseCollection;
        }

        int offsetReplaced = 0;
        foreach (var match in matches)
        {
            int offsetMatchIndex = offsetReplaced + match.MatchIndex;

            if (offsetMatchIndex < 0)
            {
                continue;
            }
            int target = offsetMatchIndex + match.MatchLength;
            // matches are from whole line, which can be truncated for displaytext.
            
            for (int i = offsetMatchIndex; i < target && i < states.Length; i++)
            {
                states[i] ??= new CharState();
                if (i == offsetMatchIndex && !string.IsNullOrEmpty(match.Replacement))
                {
                    states[i]!.ReplacedFrom = match.Replacement;
                    int offset = match.MatchLength - match.Replacement.Length;
                    offsetReplaced += offset;
                }
                if (match.IsRegexSubgroup)
                {
                    states[i]!.BackGroundState = CharState.HighLightState.RegexGroup;
                }
                else
                {
                    if (isForReplacement && match.Replacement == null)
                    {
                        if (states[i]!.BackGroundState != CharState.HighLightState.WordMatch)
                        {
                            states[i]!.BackGroundState = CharState.HighLightState.DimWordMatch;
                        }
                    }
                    else
                    {
                        states[i]!.BackGroundState = CharState.HighLightState.WordMatch;
                    }
                }
            }
        }

        CharState? stateStart = states[0] ?? new CharState();
        Border? highlightBorder = null;

        void AppendRunToCurrent(int i)
        {
            int startIndex = 0;
            IBrush foreground;
            if (stateStart != null)
            {
                startIndex = stateStart.Index;
                foreground = stateStart.Foreground;
            }
            else
            {
                foreground = Brushes.Aqua;
            }
            //Always add some text from start to current
            //currentCollection would be the baseCollection if no highlights were happening.
            //Either way this is the start point
            string text = lineText.Substring(startIndex, i-startIndex);

            var run = new Run(text)
            {
                BaselineAlignment = isForReplacement ? BaselineAlignment.Bottom : BaselineAlignment.Center,
                Foreground = foreground
            };
                
            //Add text up to this state
            currentCollection.Add(run);
        }
        
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] == null)
            {
                continue;
            }
            var state = states[i];
            if (state == null)
            {
                continue;
            }

            //isForReplacement = !string.IsNullOrEmpty(state.ReplacedFrom);
            bool highlightChanged = stateStart != null && state.BackGroundState != stateStart.BackGroundState;
            bool colorChanged = !Equals(state.Foreground, stateStart?.Foreground);
            if( !highlightChanged && !colorChanged) continue;
            
            AppendRunToCurrent(i);

            if (highlightChanged)
            {
                //begin new highlight.
                currentCollection = [];
                var block = new TextBlock{Inlines =currentCollection, VerticalAlignment = isForReplacement? VerticalAlignment.Bottom: VerticalAlignment.Center};
                var borderBrush = isForReplacement
                    ? Configuration.Instance.CurrentTheme.ContentHighlightReplaceBorder
                    : Configuration.Instance.CurrentTheme.ContentHighlightBorder;
                var backgroundBrush = isForReplacement
                    ? Configuration.Instance.CurrentTheme.ContentHighlightReplaceBackground
                    : Configuration.Instance.CurrentTheme.ContentHighlightBackground;

                    if (state.BackGroundState == CharState.HighLightState.None)
                    {
                        currentCollection = baseCollection;
                        highlightBorder = null;
                    }
                    else
                    {
                        bool extendingBorder = highlightBorder != null && !highlightChanged;
                        if (highlightBorder != null)
                        {
                            if (state.BackGroundState != CharState.HighLightState.RegexGroup)
                            {
                                //remove RightBoarder from prior highlight
                              //  highlightBorder.BorderThickness = new Thickness( highlightBorder.BorderThickness.Left ,1,0,1);
                            }
                        }
                        
                        IBrush bgbrush = state.BackGroundState == CharState.HighLightState.RegexGroup ? Brushes.Transparent : new SolidColorBrush(backgroundBrush);
                        
                        var stackPanel = new StackPanel() { Orientation = Orientation.Vertical, VerticalAlignment = isForReplacement? VerticalAlignment.Bottom: VerticalAlignment.Center};
        
                        if (!string.IsNullOrEmpty(state.ReplacedFrom))
                        {
                            var replaceRun = new Run(state.ReplacedFrom){BaselineAlignment = isForReplacement ? BaselineAlignment.Bottom: BaselineAlignment.Center};
                            var replaceInlines = new InlineCollection { replaceRun };
                            var replaceBlock = new TextBlock{Inlines =replaceInlines, VerticalAlignment = VerticalAlignment.Center};
                            var replaceeBorder = new Border
                            {
                                Padding = new Thickness(-1, 0, -1, -1 ),
                                BorderThickness = isForReplacement ? new Thickness(1,1,1,0) : new Thickness(1), 
                                BorderBrush = new SolidColorBrush(Configuration.Instance.CurrentTheme.ContentHighlightBorder), 
                                Background =  new SolidColorBrush(Configuration.Instance.CurrentTheme.ContentHighlightBackground),
                                Child = replaceBlock,
                            };
                            stackPanel.Children.Add(replaceeBorder);
                        }

                        bool centerVerticalAlign = !isForReplacement || string.IsNullOrEmpty(states[i]!.ReplacedFrom);
                        
                        //close begin new border
                        highlightBorder = new Border
                        {
                            Padding = new Thickness(-1, -1),
                            BorderThickness = extendingBorder? new Thickness(0,1,1,1): new Thickness(1), 
                            BorderBrush = new SolidColorBrush(borderBrush), 
                            Background = bgbrush,
                            Opacity = states[i]!.BackGroundState == CharState.HighLightState.DimWordMatch ? 0.7 : 1,
                            Child = block,
                            // Opacity = opacityFromText,
                            VerticalAlignment = centerVerticalAlign? VerticalAlignment.Center: VerticalAlignment.Bottom
                        };
                        stackPanel.Children.Add(highlightBorder);

                        var container = new InlineUIContainer();
                        //container.BaselineAlignment = centerVerticalAlign ? BaselineAlignment.Bottom: BaselineAlignment.Center;
                        container.BaselineAlignment = BaselineAlignment.Bottom;
                        container.Child = stackPanel;
                        baseCollection.Add(container);
                    }
            }
            
            if (i < states.Length)
            {
                stateStart = states[i];
            }
        }
        
        AppendRunToCurrent(states.Length);
        return baseCollection;
    }
}