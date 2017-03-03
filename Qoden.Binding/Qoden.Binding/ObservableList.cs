using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;
#pragma warning disable CS1701 // Assuming assembly reference matches identity

namespace Qoden.Binding
{
	public class ObservableList<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		SimpleMonitor _monitor;

		public ObservableList(IEnumerable<T> collection) : this()
		{
			if (collection == null)
			{
				throw new ArgumentNullException(nameof(collection));
			}
			CopyFrom(collection);
		}

		public ObservableList(List<T> list) : this((IEnumerable<T>)list)
		{
		}

		public ObservableList()
		{
			_monitor = new SimpleMonitor();
		}

		//
		// Methods
		//
		protected IDisposable BlockReentrancy()
		{
			_monitor.Enter();
			return _monitor;
		}

		protected void CheckReentrancy()
		{
			if (_monitor.Busy && CollectionChanged != null && CollectionChanged.GetInvocationList().Length > 1)
			{
				throw new InvalidOperationException("Cannot change ObservableCollection during a CollectionChanged event.");
			}
		}

		protected override void ClearItems()
		{
			CheckReentrancy();
			base.ClearItems();
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			OnCollectionReset();
		}

		void CopyFrom(IEnumerable<T> collection)
		{
			IList<T> items = Items;
			if (collection != null && items != null)
			{
				using (IEnumerator<T> enumerator = collection.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						items.Add(enumerator.Current);
					}
				}
			}
		}

		protected override void InsertItem(int index, T item)
		{
			CheckReentrancy();
			base.InsertItem(index, item);
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		}

		public void Move(int oldIndex, int newIndex)
		{
			MoveItem(oldIndex, newIndex);
		}

		protected virtual void MoveItem(int oldIndex, int newIndex)
		{
			CheckReentrancy();
			T t = base[oldIndex];
			RemoveItem(oldIndex);
			base.InsertItem(newIndex, t);
			OnPropertyChanged("Item[]");
			OnCollectionChanged(NotifyCollectionChangedAction.Move, t, newIndex, oldIndex);
		}

		public void AddRange(IEnumerable<T> collection)
		{
			InsertRange(Items.Count, collection);
		}

		public void InsertRange(int idx, IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			if (idx > Items.Count)
				throw new ArgumentOutOfRangeException();
			CheckReentrancy();
			foreach (var i in collection)
				Items.Add(i);
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			var list = new List<T>();
			list.AddRange(collection);
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, idx);
			OnCollectionChanged(args);
		}

		public void RemoveRange(int start, int count)
		{
			if (start < 0)
				throw new ArgumentNullException(nameof(start));
			if (count < 0)
				throw new ArgumentNullException(nameof(count));
			if (start + count > Items.Count)
				throw new ArgumentOutOfRangeException();
			CheckReentrancy();
			T[] items = new T[count];
			for (int i = 0, j = start; i < count; ++i, ++j)
			{
				items[i] = Items[j];
				Items.RemoveAt(j);
			}
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, start);
			OnCollectionChanged(args);
		}

		public void Reset(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			CheckReentrancy();
			Items.Clear();
			foreach (var i in collection)
				Items.Add(i);
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			OnCollectionReset();
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
			{
				using (BlockReentrancy())
				{
					CollectionChanged(this, e);
				}
			}
		}

		void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
		}

		void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
		}

		void OnCollectionReset()
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, e);
			}
		}

		void OnPropertyChanged(string propertyName)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		protected override void RemoveItem(int index)
		{
			CheckReentrancy();
			T t = base[index];
			base.RemoveItem(index);
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, t, index);
		}

		protected override void SetItem(int index, T item)
		{
			CheckReentrancy();
			T t = base[index];
			base.SetItem(index, item);
			OnPropertyChanged("Item[]");
			OnCollectionChanged(NotifyCollectionChangedAction.Replace, t, item, index);
		}

		//
		// Events
		//
		public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

		protected virtual event PropertyChangedEventHandler PropertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { PropertyChanged += value; }
			remove { PropertyChanged -= value; }
		}

		class SimpleMonitor : IDisposable
		{
			int _busyCount;

			public bool Busy
			{
				get { return _busyCount > 0; }
			}

			public void Enter()
			{
				_busyCount++;
			}

			public void Dispose()
			{
				_busyCount--;
			}
		}
	}
}

