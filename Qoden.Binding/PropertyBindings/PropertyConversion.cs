using System;
using System.Collections;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{

	public static class PropertyConversion
	{
		class ConvertedProperty<T, S> : IProperty<T>
		{
			readonly IProperty<S> source;
			readonly Func<S, T> toTarget;
			readonly Func<T, S> toSource;

			public ConvertedProperty (
				IProperty<S> source,
				Func<S, T> toTargetConversion,
				Func<T, S> toSourceConversion)
			{
				Assert.Argument (source, "source").NotNull ();
				Assert.Argument (toTargetConversion, "toTargetConversion").NotNull ();
				this.source = source;
				toTarget = toTargetConversion;
				toSource = toSourceConversion;
			}

			public object Owner { get { return source.Owner; } }

			public T Value {
				get { return toTarget (source.Value); }
				set {
					if (IsReadOnly) throw new InvalidOperationException ("Cannot update readonly property");
					source.Value = toSource (value);
				}
			}

			object IProperty.Value {
				get { return Value; }
				set { Value = (T)value; }
			}

			public bool IsReadOnly { get { return toSource == null; } }

			public bool HasErrors { get { return source.HasErrors; } }

			public IEnumerable Errors { get { return source.Errors; } }

			public string Key { get { return source.Key; } }

			public Type PropertyType { get { return typeof (T); } }

			IDisposable IProperty.OnPropertyChange (Action<IProperty> action)
			{
				return source.OnPropertyChange (action);
			}

			public IPropertyBindingStrategy BindingStrategy {
				get { return source.BindingStrategy; }
			}
		}

		/// <summary>
		/// Create property whose value is value of source property converted with given functions.
		/// </summary>
		/// <param name="source">source property</param>
		/// <param name="toTargetConversion">Function to convert from source to target</param>
		/// <param name="toSourceConversion">Backwards conversion. If null then converted property throw exception if value is set.</param>
		/// <typeparam name="T">Target type</typeparam>
		/// <typeparam name="S">Source type</typeparam>
		public static IProperty<T> Convert<T, S> (
			this IProperty<S> source,
			Func<S, T> toTargetConversion,
			Func<T, S> toSourceConversion = null)
		{
			return new ConvertedProperty<T, S> (source, toTargetConversion, toSourceConversion);
		}
	}

}