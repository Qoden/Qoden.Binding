using System;

namespace Qoden.Binding
{

	public delegate void BindingAction<T> (IProperty<T> target, IProperty<T> source);

	public delegate void SourcePropertyAction<SP> (IProperty<SP> source);

	public delegate void BindingAction (IProperty target, IProperty source);

	public interface IBinding
	{
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="IBinding"/> is enabled.
		/// </summary>
		bool Enabled { get; set; }

		/// <summary>
		/// Start listening to events
		/// </summary>
		void Bind ();

		/// <summary>
		/// Stop listening to events
		/// </summary>
		void Unbind ();

		/// <summary>
		/// Gets a value indicating whether this <see cref="IBinding"/> is bound.
		/// </summary>
		bool Bound { get; }

		/// <summary>
		/// Move data from source object to target.
		/// </summary>
		void UpdateTarget ();

		/// <summary>
		/// Move data from target object to source.
		/// </summary>
		void UpdateSource ();
	}
}