using Avalonia;

namespace Blitz_Behavior;


    public static class ColumnDefinition
    {
        private readonly static Avalonia.Controls.GridLength ZeroWidth = 
           new Avalonia.Controls.GridLength(0,Avalonia.Controls.GridUnitType.Pixel);

        private readonly static AttachedProperty<Avalonia.Controls.GridLength?> LastWidthProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, Avalonia.Controls.GridLength?>("LastWidth"
                , typeof(ColumnDefinition)
                , default );

        public readonly static AttachedProperty<bool> IsVisibleProperty =
             AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, bool>("IsVisible"
                 , typeof(ColumnDefinition)
                 , true
                 , coerce: ((element, visibility) =>
                      {

                          var lastWidth = element.GetValue(ColumnDefinition.LastWidthProperty);
                          if (visibility == true && lastWidth is { })
                          {
                              element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, lastWidth);
                          }
                          else if (visibility == false)
                          {
                              element.SetValue(LastWidthProperty, element.GetValue(Avalonia.Controls.ColumnDefinition.WidthProperty));
                              element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, ZeroWidth);
                          }
                          return visibility;
                      })
                 );

        public static bool GetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition)
        {
            return columnDefinition.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition, bool visibility)
        {
             columnDefinition.SetValue(IsVisibleProperty,visibility);
        }
    }

    public static class RowDefinition
    {
        private readonly static Avalonia.Controls.GridLength ZeroHeight = 
            new Avalonia.Controls.GridLength(0,Avalonia.Controls.GridUnitType.Pixel);

        private readonly static AttachedProperty<Avalonia.Controls.GridLength?> LastHeightProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.RowDefinition, Avalonia.Controls.GridLength?>("LastHeight"
                , typeof(RowDefinition)
                , default );

        public readonly static AttachedProperty<bool> IsVisibleProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.RowDefinition, bool>("IsVisible"
                , typeof(RowDefinition)
                , true
                , coerce: ((element, visibility) =>
                {

                    var lastHeight = element.GetValue(RowDefinition.LastHeightProperty);
                    if (visibility == true && lastHeight is { })
                    {
                        element.SetValue(Avalonia.Controls.RowDefinition.HeightProperty, lastHeight);
                    }
                    else if (visibility == false)
                    {
                        element.SetValue(LastHeightProperty, element.GetValue(Avalonia.Controls.RowDefinition.HeightProperty));
                        element.SetValue(Avalonia.Controls.RowDefinition.HeightProperty, ZeroHeight);
                    }
                    return visibility;
                })
            );

        public static bool GetIsVisible(Avalonia.Controls.RowDefinition rowDefinition)
        {
            return rowDefinition.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(Avalonia.Controls.RowDefinition rowDefinition, bool visibility)
        {
            rowDefinition.SetValue(IsVisibleProperty,visibility);
        }
    }
