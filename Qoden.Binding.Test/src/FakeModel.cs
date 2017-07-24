using System;
using Qoden.Validation;

namespace Qoden.Binding.Test
{
    public class FakeModel : DataContext
    {
        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                Validator.CheckProperty(value).NotEmpty();
                _name = value;
                RaisePropertyChanged();
            }
        }
    }
}
