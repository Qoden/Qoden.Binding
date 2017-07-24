using System;
using System.ComponentModel;
using Qoden.Reflection;

namespace Qoden.Binding.Test
{
    public class FakeUIControl : INotifyPropertyChanged
    {
        string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Text"));
                TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> TextChanged;

        public IProperty<string> TextProperty()
        {
            return this.GetProperty(_ => _.Text, TextChangedBinding);
        }

        public static readonly RuntimeEvent TextChangedEvent = new RuntimeEvent(typeof(FakeUIControl), "TextChanged");
        public static readonly IPropertyBindingStrategy TextChangedBinding = new EventHandlerBindingStrategy<EventArgs>(TextChangedEvent);
    }
}
