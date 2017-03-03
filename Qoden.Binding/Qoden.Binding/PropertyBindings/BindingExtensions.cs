using System;

namespace Qoden.Binding
{

	public static class BindingExtensions
	{
		static void DoNotUpdate (IProperty target, IProperty source)
		{
		}

		public static void DontUpdateTarget (this IPropertyBinding binding)
		{
			binding.UpdateTargetAction = DoNotUpdate;
		}

		public static void DontUpdateSource (this IPropertyBinding binding)
		{
			binding.UpdateSourceAction = DoNotUpdate;
		}

		public static bool UpdatesSource (this IPropertyBinding binding)
		{
			return binding.UpdateSourceAction != DoNotUpdate;
		}

		public static bool UpdatesTarget (this IPropertyBinding binding)
		{
			return binding.UpdateTargetAction != DoNotUpdate;
		}
	}

}