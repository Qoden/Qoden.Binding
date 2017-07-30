using System;
using System.ComponentModel;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
    public interface IObjectBindingStrategy
    {
        IDisposable SubscribeToChanges(object @object, PropertyChangedEventHandler action);
    }

    public class NotifyPropertyChangedStrategy : IObjectBindingStrategy
    {
        public static readonly RuntimeEvent PropertyChanged = new RuntimeEvent(typeof(INotifyPropertyChanged), "PropertyChanged");
        public static readonly NotifyPropertyChangedStrategy Instance = new NotifyPropertyChangedStrategy();

        public IDisposable SubscribeToChanges(object @object, PropertyChangedEventHandler action)
        {
            Assert.Argument(@object, "object").NotNull();
            var o = Assert.Argument(@object as INotifyPropertyChanged, "object")
                          .NotNull("Object does not implement INotifyPropertyChanged")
                          .Value;
            Assert.Argument(action, "action").NotNull();
            return new EventSubscription(PropertyChanged, action, @object);
        }
    }

    public class ObjectBinding : IBinding
    {
        IDisposable sourceSubscription;

        INotifyPropertyChanged source;

        public INotifyPropertyChanged Source
        {
            get { return source; }
            set
            {
                Assert.Property(value).NotNull();
                Assert.State(Bound).IsFalse("Cannot change Source of already Bound binding");
                source = value;
            }
        }

        public bool Enabled { get; set; }

        public bool Bound
        {
            get { return sourceSubscription != null; }
        }

        IObjectBindingStrategy _bindingStrategy = NotifyPropertyChangedStrategy.Instance;
        public IObjectBindingStrategy BindingStrategy 
        { 
            get => _bindingStrategy; 
            set 
            {
                Assert.Property(value).NotNull();
                _bindingStrategy = value;   
            }
        }

        public void Bind()
        {
            if (Bound)
                return;
            Assert.State(Source != null).IsTrue("Source is not set");
            sourceSubscription = BindingStrategy.SubscribeToChanges(Source, Source_Change);
        }

        void Source_Change(object sender, PropertyChangedEventArgs e)
        {
            if (Enabled && PropertyChanged != null)
            {
                var handler = PropertyChanged;
                handler.Invoke(Source, e);
            }
        }

        public void Unbind()
        {
            if (!Bound) return;
            sourceSubscription.Dispose();
            sourceSubscription = null;
        }

        public void UpdateSource()
        {
        }

        public void UpdateTarget()
        {
            Source_Change(Source, new PropertyChangedEventArgs(""));
        }

        PropertyChangedEventHandler _propertyChanged;
        public PropertyChangedEventHandler PropertyChanged 
        { 
            get => _propertyChanged; 
            set
            {
                Assert.Property(value).NotNull();
                _propertyChanged = value;
            }
        }
    }

    public static class ObjectBinding_BindingList_Extensions 
    {
        public static void Object(this BindingList list, INotifyPropertyChanged @object, PropertyChangedEventHandler handler)
        {
            list.Add(new ObjectBinding
            {
                Source = @object,
                PropertyChanged = handler
            });
        }
    }
}
