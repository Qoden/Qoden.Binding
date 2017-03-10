using System.Collections.Generic;
using System.Linq;
using Qoden.Validation;

namespace Qoden.Binding
{
	/// <summary>
	/// Convenient class to manage multiple bindings at aonce.
	/// </summary>
	/// <remarks>
	/// Usually you can create one binding list per class or per view model to store all bindings related to it. 
	/// This way everything get enabled/disabled, bound/unbound at once.
	/// </remarks>
	public class BindingList : IBinding
	{
		readonly List<IBinding> _bindings = new List<IBinding> ();

		/// <summary>
		/// Add new binding to the list
		/// </summary>
		/// <param name="binding">Binding to add</param>
		public void Add (IBinding binding)
		{
			Assert.Argument (binding, "binding").NotNull ();
			binding.Enabled = Enabled;
			_bindings.Add (binding);
            if (Bound)
            {
                binding.Bind();
                binding.UpdateTarget();
            }
        }

		/// <summary>
		/// Remove the specified binding.
		/// </summary>
		/// <param name="binding">Binding to remove</param>
		public void Remove (IBinding binding)
		{
			Assert.Argument (binding, "binding").NotNull ();
			if (_bindings.Remove (binding)) {
				binding.Unbind ();
			}
		}

		/// <summary>
		/// Unbind and clear all bindings
		/// </summary>
        public void Clear()
        {
            Unbind();
            _bindings.Clear();
        }

		/// <summary>
		/// Call <see cref="IBinding.UpdateTarget"/> for all bindings in the list
		/// </summary>
		public void UpdateTarget ()
		{
			_bindings.ForEach (_ => _.UpdateTarget ());
		}

		/// <summary>
		/// Call <see cref="IBinding.UpdateSource"/> for all bindings in the list
		/// </summary>
		public void UpdateSource ()
		{
			_bindings.ForEach (_ => _.UpdateSource ());
		}

		/// <summary>
		/// Call <see cref="IBinding.Bind"/> for all bindings in the list
		/// </summary>
		public void Bind ()
		{
            _bindings.ForEach (_ => _.Bind ());
            Bound = true;
		}

		/// <summary>
		/// Call <see cref="IBinding.Unbind"/> for all bindings in the list
		/// </summary>
		public void Unbind ()
		{
            _bindings.ForEach (_ => _.Unbind ());
            Bound = false;
		}

		/// <summary>
		/// Enable/Disable all bindings in the list. Property value is false if any of bidnings is disabled.
		/// </summary>
		public bool Enabled { 
			get { return _bindings.All (_ => _.Enabled); }
			set { _bindings.ForEach (_ => _.Enabled = value); }
		}

		/// <summary>
		/// Indicate the this list is bound. 
		/// </summary>
		/// <value><c>true</c> if bound; otherwise, <c>false</c>.</value>
		public bool Bound { get; private set; }

		/// <summary>
		/// Get number of bindings in the list.
		/// </summary>
        public int Count { get { return _bindings.Count; } }
	}

    public class WeakBindingList : IBinding
    {
        readonly WeakCollection<IBinding> bindings = new WeakCollection<IBinding>();

        public void Add(IBinding binding)
        {
            Assert.Argument(binding, "binding").NotNull();
            binding.Enabled = Enabled;
            bindings.Add(binding);
            if (Bound)
            {
                binding.Bind();
                binding.UpdateTarget();
            }
        }

        public void Remove(IBinding binding)
        {
            Assert.Argument(binding, "binding").NotNull();
            if (bindings.Remove(binding))
            {
                binding.Unbind();
            }
        }

        public void Clear()
        {
            Unbind();
            bindings.Clear();
        }

        public void UpdateTarget()
        {
            bindings.ForEach(_ => _.UpdateTarget());
        }

        public void UpdateSource()
        {
            bindings.ForEach(_ => _.UpdateSource());
        }

        public void Bind()
        {
            bindings.ForEach(_ => _.Bind());
            Bound = true;
        }

        public void Unbind()
        {
            bindings.ForEach(_ => _.Unbind());
            Bound = false;
        }

        public bool Enabled
        {
            get { return bindings.All(_ => _.Enabled); }
            set { bindings.ForEach(_ => _.Enabled = value); }
        }

        public bool Bound { get; private set; }

        public int Count { get { return bindings.CompleteCount; } }
    }
}