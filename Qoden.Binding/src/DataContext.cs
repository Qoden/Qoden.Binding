using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using Qoden.Validation;
using Qoden.Reflection;
using System.Reflection;

namespace Qoden.Binding
{
    public interface IDataContext : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        IValidator Validator { get; }

        bool Validate();
    }

    /// <summary>
    /// Holds data and objects to be displayed or edited.
    /// </summary>
    /// <remarks>
    /// <see cref="DataContext"/> provides minimal infrastructure for interactive object editing. 
    /// </remarks>
    public class DataContext : IDataContext, IEditableObject
    {
        Validator _validator;
        Dictionary<string, object> _originals;
        IKeyValueCoding _kvc;

        public DataContext()
        {
            _kvc = KeyValueCoding.Impl(GetType());
        }

        protected void RememberAndBeginEdit([CallerMemberName] string key = null)
        {
            if (!Editing)
            {
                BeginEdit();
            }
            Remember(key);
        }

        protected void Remember([CallerMemberName] string key = null)
        {
            if (Editing)
            {
                if (!_originals.ContainsKey(key))
                {
                    _originals[key] = _kvc.Get(this, key);
                    if (_originals.Count == 1)
                    {
                        RaisePropertyChanged("HasChanges");
                    }
                }
            }
        }

        public IReadOnlyDictionary<string, object> Changes
        {
            get { return _originals; }
        }

        public bool Editing
        {
            get => _originals != null;
        }

        public bool HasChanges
        {
            get => _originals != null && _originals.Count > 0;
        }

        public void BeginEdit()
        {
            if (!Editing)
            {
                _originals = new Dictionary<string, object>();
                OnBeginEdit();
                RaisePropertyChanged("Editing");
            }
        }

        protected virtual void OnBeginEdit()
        {
        }

        public void CancelEdit()
        {
            if (Editing)
            {
                OnCancelEdit();
                foreach (var kv in _originals)
                {
                    _kvc.Set(this, kv.Key, kv.Value);
                }
                _originals = null;
                RaisePropertyChanged("HasChanges");
                RaisePropertyChanged("Editing");
            }
        }

        protected virtual void OnCancelEdit()
        {
        }

        public void EndEdit()
        {
            if (Editing)
            {
                OnEndEdit();
                _originals = null;
                RaisePropertyChanged("HasChanges");
                RaisePropertyChanged("Editing");
            }
        }

        protected virtual void OnEndEdit()
        {
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Validating || EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Get <see cref="DataContext"/> Validator
        /// </summary>
        public IValidator Validator
        {
            get
            {
                if (_validator == null)
                {
                    _validator = new Validator();
                    if (!Validating)
                    {
                        Validate();
                    }
                }
                return _validator;
            }
        }

        /// <summary>
        /// Indicate if validation is in process. See <see cref="Validate"/> for details.
        /// </summary>
	    public bool Validating { get; private set; }

        /// <summary>
        /// Validate <see cref="DataContext"/> by calling all property setters defined in it. 
        /// Make sure your setters ready to be called during validation. See remarks for details.
        /// </summary>
        /// <remarks>
        /// This method does following
        /// 1. Transition object into Validating state
        /// 2. Call all properties not marked with <see cref="DontCallDuringValidationAttribute"/> to trigger validation errors.
        /// 3. Call <see cref="OnValidate"/> after properties processed
        /// 4. Fire <see cref="PropertyChanged"/> for every property with errors.
        /// 5. Transition back to normal state.
        /// </remarks>
        /// <returns>true if validation successfull (no errors found)</returns>
		public bool Validate()
        {
            Validating = true;
            var properties = Inspection.InstanceProperties(GetType());
            try
            {
                Validator.Clear();
                //trigger all properties to generate errors
                foreach (var p in properties)
                {
                    if (!p.HasAttribute<DontCallDuringValidationAttribute>() && p.CanWrite && p.CanRead)
                    {
                        var val = _kvc.Get(this, p.Name);
                        _kvc.Set(this, p.Name, val);
                    }
                }
                OnValidate();
            }
            finally
            {
                foreach (var p in properties)
                {
                    if (Validator.HasErrorsForKey(p.Name))
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p.Name));
                    }
                }
                Validating = false;
            }
            return Validator.HasErrors;
        }

        /// <summary>
        /// Called after <see cref="DataContext"/> properties validated. See <see cref="Validate"/> for details.
        /// </summary>
		protected virtual void OnValidate()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise <see cref="PropertyChanged"/> event with provided args
        /// </summary>
        /// <param name="key">event arguments</param>
		protected void RaisePropertyChanged(PropertyChangedEventArgs key)
        {
            if (!Validating)
            {
                PropertyChanged?.Invoke(this, key);
            }
        }

        /// <summary>
        /// Convenience method to raise <see cref="PropertyChanged"/> event inside <see cref="DataContext"/> properties.
        /// </summary>
        /// <param name="key">property name, autoamtically generated by compiler</param>
        protected void RaisePropertyChanged([CallerMemberName] string key = null)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(key));
        }

        /// <summary>
        /// Raise <see cref="PropertyChanged"/> event for provided property getter expression (ex: x => x.MyProperty).
        /// </summary>
        /// <typeparam name="T">type of property</typeparam>
        /// <param name="key">property expression</param>
        protected void RaisePropertyChanged<T>(Expression<Func<T>> key)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(PropertySupport.ExtractPropertyName(key)));
        }

        /// <summary>
        /// Fired when new <see cref="Validator"/> detect new error or errors removed from <see cref="Validator"/>
        /// </summary>
		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged
        {
            add { Validator.ErrorsChanged += value; }
            remove { Validator.ErrorsChanged -= value; }
        }

        /// <summary>
        /// Convenience method to get errors from <see cref="Validator"/>. Pass null as argument to get all errors.
        /// </summary>
        /// <param name="propertyName">property name</param>
        /// <returns>List of errors for given property</returns>
		public System.Collections.IEnumerable GetErrors(string propertyName = null)
        {
            return Validator.GetErrors(propertyName);
        }

        /// <summary>
        /// Indicate if <see cref="DataContext"/> has errors in it <see cref="Validator"/>.
        /// </summary>
		public bool HasErrors => Validator.HasErrors;

        private BindingList _bindings;
        public BindingList Bindings
        {
            get { return _bindings ?? (_bindings = new BindingList()); }
            set
            {
                Assert.Argument(value, "value").NotNull();
                _bindings = value;
            }
        }
    }

    public static class DataContextExtensions
    {
        public static bool HasErrorsForKey(this IDataContext obj, string key)
        {
            return obj.Validator.HasErrorsForKey(key);
        }

        public static bool HasErrorsForKey<T, TK>(this IDataContext obj, Expression<Func<T, TK>> key)
        {
            return HasErrorsForKey(obj, PropertySupport.ExtractPropertyName(key));
        }
    }
}
