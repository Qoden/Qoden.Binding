using Microsoft.VisualStudio.TestTools.UnitTesting;
using XAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Qoden.Binding.Test
{
    [TestClass]
    public class BindingBuildersTest
    {
        private BindingList bl = new BindingList();
        private FakeModel model = new FakeModel();
        private FakeUIControl control = new FakeUIControl();
        private IProperty<string> target = null, source = null;

        [TestMethod]
        public void AfterTargetUpdate()
        {
            bl.Property(model, x => x.Name).To(control.TextProperty())
              .AfterTargetUpdate((binding, s) =>
            {
                target = (IProperty<string>)binding.Target;
                source = (IProperty<string>)binding.Source;
            });
            bl.Bind();

            model.Name = "Hello World";

            XAssert.IsNotNull(target);
            XAssert.IsNotNull(source);
        }

        [TestMethod]
        public void AfterSourceUpdate()
        {
            bl.Property(model, x => x.Name).To(control.TextProperty())
              .AfterSourceUpdate((binding, s) =>
              {
                  target = (IProperty<string>)binding.Target;
                  source = (IProperty<string>)binding.Source;
              });
            bl.Bind();

            control.Text = "Hello World";

            XAssert.IsNotNull(target);
            XAssert.IsNotNull(source);
        }
    }
}
