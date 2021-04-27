using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PatternModifierUI.ViewModels
{
    public class MyRowDefinition : RowDefinition
    {
        private GridLength _height;

        public bool IsHidden
        {
            get { return (bool)GetValue(IsHiddenProperty); }
            set { SetValue(IsHiddenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsHidden.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.Register("IsHidden", typeof(bool), typeof(MyRowDefinition), new PropertyMetadata(false, Changed));

        public static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var o = d as MyRowDefinition;
            o.Toggle((bool)e.NewValue);
        }

        public void Toggle(bool isHidden)
        {
            if (isHidden)
            {
                _height = this.Height;
                this.Height = new GridLength(0, GridUnitType.Star);
            }
            else
                this.Height = _height;
        }
    }
}
