using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoden.Validation;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Qoden.Binding.Test
{
	[TestClass]
    public class DataContextTest
    {
        [TestMethod]
        public void PropertyChanges()
        {
            var ctx = new TestContext();
            Assert.IsTrue(ctx.HasErrors, "Valid/Invalid state indicated correctly right after data context instantiated");
            Assert.IsNotNull(ctx.GetErrors("Id"));
            ctx.Id = "Some Id";
            ctx.Industry = "Finance";
            ctx.Name = "Andrew";
            Assert.IsFalse(ctx.HasErrors, "Property changes affect context validtity status");
        }

		[TestMethod]
        public void PropertyChangeEvents()
        {
            var ctx = new TestContext();
            var propertyName = "";
            ctx.PropertyChanged += (o, e) => { propertyName = e.PropertyName; };
            ctx.Name = "Andrew";
            Assert.AreEqual(propertyName, "Name", "Thanks to Field support property change events raised automatically");
        }

		[TestMethod]
        public void NoPropertyChangeDuringValidation()
        {
            var ctx = ValidContext();
            var propertyName = "";
            ctx.PropertyChanged += (o, e) => { propertyName = e.PropertyName; };
            ctx.Validate();
            Assert.AreEqual(propertyName, "", "Property change events not raised during validation");
        }

        [TestMethod]
        public void EditingStatusFlags()
        {
            var ctx = ValidContext();
            Assert.IsFalse(ctx.Editing);

            ctx.BeginEdit();
            Assert.IsTrue(ctx.Editing);
            Assert.IsFalse(ctx.HasChanges);

            ctx.EndEdit();
            Assert.IsFalse(ctx.HasChanges);
            Assert.IsFalse(ctx.Editing);
        }

        [TestMethod]
        public void CancelEditRestoreStates()
        {
            var ctx = ValidContext();
            ctx.BeginEdit();

            var oldIndustru = ctx.Industry;
            ctx.Industry = "Some Industry";
            var oldName = ctx.Name;
            ctx.Name = "New Name";
            var oldId = ctx.Id;
            ctx.Id = "New ID";

            ctx.CancelEdit();

            Assert.AreEqual(oldIndustru, ctx.Industry);
            Assert.AreEqual(oldName, ctx.Name);
            Assert.AreEqual(oldId, ctx.Id);
        }

        [TestMethod]
        public void CancelEditFiresPropertyChange()
        {
            var ctx = ValidContext();
            var props = new List<string>();
            ctx.PropertyChanged += (sender, e) => { props.Add(e.PropertyName); };

            ctx.Industry = "New Industry";
            ctx.Id = "New ID";
            ctx.CancelEdit();

            CollectionAssert.AreEquivalent(new[] { "Industry", "Id" }, props);
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

        private string _id;
        private MyDto _dto;

        public TestContext()
        {
            _id = "";
            _dto = new MyDto();
        }

        public string Id
        {
            get { return _id; }
            set
            {
                Validator.CheckProperty(value)
                    .NotNull()
                    .NotEmpty();
                Remember();
                SetProperty(ref _id, value);
            }
        }

        public string Name
        {
            get { return _dto.Name; }
            set
            {
                Validator.CheckProperty(value)
                    .NotEmpty()
                    .MinLength(2)
                    .MaxLength(100);
                if (!Validating && _dto.Name != value)
                {
                    Remember();
                    _dto.Name = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Industry
        {
            get { return _dto.Industry; }
            set
            {
                Validator.CheckProperty(value)
                    .In(ValidIndsutries);
                if (!Validating && _dto.Industry != value)
                {
                    Remember();
                    _dto.Industry = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}