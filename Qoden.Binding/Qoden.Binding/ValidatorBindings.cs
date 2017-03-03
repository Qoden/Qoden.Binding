using System;
using System.Collections.Generic;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
	public static class ValidatorBindings
	{
		public static readonly RuntimeEvent ErrorsChangedEvent = new RuntimeEvent(typeof(IValidator), "ErrorsChanged");

		public static readonly IPropertyBindingStrategy ErrorsChangedBindingStrategy =
			new EventHandlerBindingStrategy(ErrorsChangedEvent);

		public static Property<IEnumerable<Error>> ErrorsProperty<T>(this T list)
			where T : IValidator
		{
			return list.GetProperty(_ => _.Errors, ErrorsChangedBindingStrategy, () => list.Errors);
		}

	}
}
