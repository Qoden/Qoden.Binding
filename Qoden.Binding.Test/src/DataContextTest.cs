using Qoden.Validation;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Qoden.Binding.Test
{
	[TestFixture]
    public class DataContextTest
    {
        [Test]
        public void PropertyChanges()
        {
            var ctx = new TestContext();
            Assert.True(ctx.HasErrors, "Valid/Invalid state indicated correctly right after data context instantiated");
            Assert.NotNull(ctx.GetErrors("Id"));
            ctx.Id = "Some Id";
            ctx.Industry = "Finance";
            ctx.Name = "Andrew";
            Assert.False(ctx.HasErrors, "Property changes affect context validtity status");
        }

        [Test]
        public void PropertyChangeEvents()
        {
            var ctx = new TestContext();
            var propertyName = "";
            ctx.PropertyChanged += (o, e) => { propertyName = e.PropertyName; };
            ctx.Name = "Andrew";
            Assert.AreEqual(propertyName, "Name", "Thanks to Field support property change events raised automatically");
        }

        [Test]
        public void NoPropertyChangeDuringValidation()
        {
            var ctx = ValidContext();
            var propertyName = "";
            ctx.PropertyChanged += (o, e) => { propertyName = e.PropertyName; };
            ctx.Validate();
            Assert.AreEqual(propertyName, "", "Property change events not raised during validation");
        }

		private TestContext ValidContext()
		{
			return new TestContext
			{
				Id = "Some Id",
				Industry = "Finance",
				Name = "Andrew"
			};
		}
    }

    public class MyDto
    {
        public string Name { get; set; }
        public string Industry { get; set; }
    }

    public class TestContext : DataContext
    {
        public static string[] ValidIndsutries = {"Finance", "Healthcare", "Education"};

        private readonly Field<string> _id;
        private readonly Field<MyDto> _dto;

        public TestContext()
        {
            _id = FieldValue<string>();
            _dto = FieldValue(new MyDto());
        }

        public string Id
        {
            get { return _id; }
            set
            {
                Validator.CheckProperty(value)
                    .NotNull()
                    .NotEmpty();
                _id.Value = value;
            }
        }

        public string Name
        {
            get { return _dto.Get<string>(); }
            set
            {
                Validator.CheckProperty(value)
                    .NotEmpty()
                    .MinLength(2)
                    .MaxLength(100);
                _dto.Set(value);
            }
        }

        public string Industry
        {
            get { return _dto.Get<string>(); }
            set
            {
                Validator.CheckProperty(value)
                    .In(ValidIndsutries);
                _dto.Set(value);
            }
        }
    }
}