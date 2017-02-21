using System;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
	public interface IPropertyBinding : IBinding
	{
		/// <summary>
		/// Get source object property
		/// </summary>
		IProperty Source { get; set; }

		/// <summary>
		/// Gets target object property.
		/// </summary>
		IProperty Target { get; set; }

		/// <summary>
		/// Action to move data from Source property to Target. By default it perform 
		/// <code>
		/// Target.FieldValue = Source.FieldValue
		/// </code>
		/// </summary>
		BindingAction UpdateTargetAction { get; set; }

		/// <summary>
		/// Action to move data from Target property to Source. By default it perform 
		/// <code>
		/// Source.FieldValue = Target.FieldValue
		/// </code>
		/// </summary>
		/// <value>The target to source action.</value>
		BindingAction UpdateSourceAction { get; set; }
	}

	public class PropertyBinding : IPropertyBinding
	{
		IDisposable sourceSubscription;
		IDisposable targetSubscription;

		public PropertyBinding ()
		{
			Enabled = true;
			updateSource = DefaultUpdateSource;
			updateTarget = DefaultUpdateTarget;
		}

		static void DefaultUpdateSource (IProperty target, IProperty source)
		{
			if (!source.IsReadOnly) {
				source.Value = target.Value;
			}
		}

		static void DefaultUpdateTarget (IProperty target, IProperty source)
		{
			if (!target.IsReadOnly) {
				target.Value = source.Value;
			}
		}

		public void Bind ()
		{
			if (Bound)
				return;
			Assert.State (Source != null).IsTrue ("Source is not set");
			sourceSubscription = Source.OnPropertyChange (Source_Change);
			if (Target != null)
				targetSubscription = Target.OnPropertyChange (Target_Change);
		}

		void Source_Change (IProperty _)
		{
			UpdateTarget ();
		}

		void Target_Change (IProperty _)
		{
			UpdateSource ();
		}

		public void Unbind ()
		{
			if (!Bound)
				return;
			sourceSubscription.Dispose ();
			sourceSubscription = null;
			if (targetSubscription != null) {
				targetSubscription.Dispose ();
				targetSubscription = null;
			}
		}

		public bool Bound {
			get { return sourceSubscription != null; }
		}

		public void UpdateTarget ()
		{	
			if (this.UpdatesTarget ())
				PerformAction (UpdateTargetAction);
		}

		public void UpdateSource ()
		{				
			if (this.UpdatesSource ())
				PerformAction (UpdateSourceAction);
		}

		bool performingAction;

		void PerformAction (BindingAction action)
		{
			if (Enabled && !performingAction) {
				Assert.State (Source != null).IsTrue ("Source is not set");
				try {
					performingAction = true;
					action (Target, Source);
				} finally {
					performingAction = false;
				}
			}
		}

		IProperty source;

		public IProperty Source {
			get { return source; }
			set { 
				Assert.Argument (value, "value").NotNull ();
				Assert.State (Bound).IsFalse ();
				source = value;
			}
		}

		IProperty target;

		public IProperty Target {
			get { return target; }
			set {
				Assert.Argument (value, "value").NotNull ();
				Assert.State (Bound).IsFalse ();
				target = value;
			}
		}

		BindingAction updateTarget;

		public BindingAction UpdateTargetAction {
			get { return updateTarget; }
			set { 
				Assert.Argument (value, "value").NotNull ();
				updateTarget = value;
			}
		}

		BindingAction updateSource;

		public BindingAction UpdateSourceAction {
			get { return updateSource; }
			set { 
				Assert.Argument (value, "value").NotNull ();
				updateSource = value;
			}
		}

		public bool Enabled { get; set; }
	}

}