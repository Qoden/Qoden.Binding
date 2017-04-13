/**
 * This test is adopted from Mono project.
 **/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Qoden.Binding;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable RECS0026 // Possible unassigned object created by 'new'			
namespace Qoden.Binding.Test
{
	[TestClass]
	public class ObservableListTest
	{
		[TestMethod]
		public void Constructor()
		{
			var list = new List<int> { 3 };
			var col = new ObservableList<int>(list);
			col.Add(5);
			Assert.AreEqual(1, list.Count, "#1");

			col = new ObservableList<int>((IEnumerable<int>)list);
			col.Add(5);
			Assert.AreEqual(1, list.Count, "#2");
		}

		[TestMethod]
		public void Constructor_Invalid()
		{

			try
			{
#pragma warning disable IDE0004 // Remove Unnecessary Cast
				new ObservableList<int>((List<int>)null);
#pragma warning restore IDE0004 // Remove Unnecessary Cast
				Assert.Fail("#1");
			}
			catch (ArgumentNullException)
			{
			}

			try
			{
				new ObservableList<int>((IEnumerable<int>)null);
				Assert.Fail("#2");
			}
			catch (ArgumentNullException)
			{
			}
		}

		[TestMethod]
		public void Insert()
		{
			bool reached = false;
			ObservableList<int> col = new ObservableList<int>();
			col.CollectionChanged += (sender, e) =>
			{
				reached = true;
				Assert.AreEqual(NotifyCollectionChangedAction.Add, e.Action, "INS_1");
				Assert.AreEqual(0, e.NewStartingIndex, "INS_2");
				Assert.AreEqual(-1, e.OldStartingIndex, "INS_3");
				Assert.AreEqual(1, e.NewItems.Count, "INS_4");
				Assert.AreEqual(5, (int)e.NewItems[0], "INS_5");
				Assert.AreEqual(null, e.OldItems, "INS_6");
			};
			col.Insert(0, 5);
			Assert.IsTrue(reached, "INS_5");
		}

		[TestMethod]
		public void RemoveAt()
		{
			bool reached = false;
			ObservableList<int> col = new ObservableList<int>();
			col.Insert(0, 5);
			col.CollectionChanged += (sender, e) =>
			{
				reached = true;
				Assert.AreEqual(NotifyCollectionChangedAction.Remove, e.Action, "REMAT_1");
				Assert.AreEqual(-1, e.NewStartingIndex, "REMAT_2");
				Assert.AreEqual(0, e.OldStartingIndex, "REMAT_3");
				Assert.AreEqual(null, e.NewItems, "REMAT_4");
				Assert.AreEqual(1, e.OldItems.Count, "REMAT_5");
				Assert.AreEqual(5, (int)e.OldItems[0], "REMAT_6");
			};
			col.RemoveAt(0);
			Assert.IsTrue(reached, "REMAT_7");
		}

		[TestMethod]
		public void Move()
		{
			bool reached = false;
			ObservableList<int> col = new ObservableList<int>();
			col.Insert(0, 0);
			col.Insert(1, 1);
			col.Insert(2, 2);
			col.Insert(3, 3);
			col.CollectionChanged += (sender, e) =>
			{
				reached = true;
				Assert.AreEqual(NotifyCollectionChangedAction.Move, e.Action, "MOVE_1");
				Assert.AreEqual(3, e.NewStartingIndex, "MOVE_2");
				Assert.AreEqual(1, e.OldStartingIndex, "MOVE_3");
				Assert.AreEqual(1, e.NewItems.Count, "MOVE_4");
				Assert.AreEqual(1, e.NewItems[0], "MOVE_5");
				Assert.AreEqual(1, e.OldItems.Count, "MOVE_6");
				Assert.AreEqual(1, e.OldItems[0], "MOVE_7");
			};
			col.Move(1, 3);
			Assert.IsTrue(reached, "MOVE_8");
		}

		[TestMethod]
		public void Add()
		{
			ObservableList<char> collection = new ObservableList<char>();
			bool propertyChanged = false;
			List<string> changedProps = new List<string>();
			NotifyCollectionChangedEventArgs args = null;

			((INotifyPropertyChanged)collection).PropertyChanged += (sender, e) =>
			{
				propertyChanged = true;
				changedProps.Add(e.PropertyName);
			};

			collection.CollectionChanged += (sender, e) =>
			{
				args = e;
			};

			collection.Add('A');

			Assert.IsTrue(propertyChanged, "ADD_1");
			Assert.IsTrue(changedProps.Contains("Count"), "ADD_2");
			Assert.IsTrue(changedProps.Contains("Item[]"), "ADD_3");

			CollectionChangedEventValidators.ValidateAddOperation(args, new char[] { 'A' }, 0, "ADD_4");
		}

		[TestMethod]
		public void Remove()
		{
			ObservableList<char> collection = new ObservableList<char>();
			bool propertyChanged = false;
			List<string> changedProps = new List<string>();
			NotifyCollectionChangedEventArgs args = null;

			collection.Add('A');
			collection.Add('B');
			collection.Add('C');

			((INotifyPropertyChanged)collection).PropertyChanged += (sender, e) =>
			{
				propertyChanged = true;
				changedProps.Add(e.PropertyName);
			};

			collection.CollectionChanged += (sender, e) =>
			{
				args = e;
			};

			collection.Remove('B');

			Assert.IsTrue(propertyChanged, "REM_1");
			Assert.IsTrue(changedProps.Contains("Count"), "REM_2");
			Assert.IsTrue(changedProps.Contains("Item[]"), "REM_3");

			CollectionChangedEventValidators.ValidateRemoveOperation(args, new char[] { 'B' }, 1, "REM_4");
		}

		[TestMethod]
		public void Set()
		{
			ObservableList<char> collection = new ObservableList<char>();
			bool propertyChanged = false;
			List<string> changedProps = new List<string>();
			NotifyCollectionChangedEventArgs args = null;

			collection.Add('A');
			collection.Add('B');
			collection.Add('C');

			((INotifyPropertyChanged)collection).PropertyChanged += (sender, e) =>
			{
				propertyChanged = true;
				changedProps.Add(e.PropertyName);
			};

			collection.CollectionChanged += (sender, e) =>
			{
				args = e;
			};

			collection[2] = 'I';

			Assert.IsTrue(propertyChanged, "SET_1");
			Assert.IsTrue(changedProps.Contains("Item[]"), "SET_2");

			CollectionChangedEventValidators.ValidateReplaceOperation(args, new char[] { 'C' }, new char[] { 'I' }, 2, "SET_3");
		}

		[TestMethod]
		public void Reentrant()
		{
			ObservableList<char> collection = new ObservableList<char>();
			bool propertyChanged = false;
			List<string> changedProps = new List<string>();
			NotifyCollectionChangedEventArgs args = null;

			collection.Add('A');
			collection.Add('B');
			collection.Add('C');

			PropertyChangedEventHandler pceh = (sender, e) =>
			{
				propertyChanged = true;
				changedProps.Add(e.PropertyName);
			};

			// Adding a PropertyChanged event handler
			((INotifyPropertyChanged)collection).PropertyChanged += pceh;

			collection.CollectionChanged += (sender, e) =>
			{
				args = e;
			};

			collection.CollectionChanged += (sender, e) =>
			{
				// This one will attempt to break reentrancy
				try
				{
					collection.Add('X');
					Assert.Fail("Reentrancy should not be allowed.");
				}
				catch (InvalidOperationException)
				{
				}
			};

			collection[2] = 'I';

			Assert.IsTrue(propertyChanged, "REENT_1");
			Assert.IsTrue(changedProps.Contains("Item[]"), "REENT_2");

			CollectionChangedEventValidators.ValidateReplaceOperation(args, new char[] { 'C' }, new char[] { 'I' }, 2, "REENT_3");

			// Removing the PropertyChanged event handler should work as well:
			((INotifyPropertyChanged)collection).PropertyChanged -= pceh;
		}

		//Private test class for protected members of ObservableList
		private class ObservableListTestHelper : ObservableList<char>
		{
			internal void DoubleEnterReentrant()
			{
				IDisposable object1 = BlockReentrancy();
				IDisposable object2 = BlockReentrancy();

				Assert.AreSame(object1, object2);

				//With double block, try the reentrant:
				NotifyCollectionChangedEventArgs args = null;

				CollectionChanged += (sender, e) =>
				{
					args = e;
				};

				// We need a second callback for reentrancy to matter
				CollectionChanged += (sender, e) =>
				{
					// Doesn't need to do anything; just needs more than one callback registered.
				};

				// Try adding - this should cause reentrancy, and fail
				try
				{
					Add('I');
					Assert.Fail("Reentrancy should not be allowed. -- #2");
				}
				catch (InvalidOperationException)
				{
				}

				// Release the first reentrant
				object1.Dispose();

				// Try adding again - this should cause reentrancy, and fail again
				try
				{
					Add('J');
					Assert.Fail("Reentrancy should not be allowed. -- #3");
				}
				catch (InvalidOperationException)
				{
				}

				// Release the reentrant a second time
				object1.Dispose();

				// This last add should work fine.
				Add('K');
				CollectionChangedEventValidators.ValidateAddOperation(args, new char[] { 'K' }, 0, "REENTHELP_1");
			}
		}

		[TestMethod]
		public void ReentrantReuseObject()
		{
			ObservableListTestHelper helper = new ObservableListTestHelper();

			helper.DoubleEnterReentrant();
		}

		[TestMethod]
		public void Clear()
		{
			List<char> initial = new List<char>();

			initial.Add('A');
			initial.Add('B');
			initial.Add('C');

			ObservableList<char> collection = new ObservableList<char>(initial);
			bool propertyChanged = false;
			List<string> changedProps = new List<string>();
			NotifyCollectionChangedEventArgs args = null;

			((INotifyPropertyChanged)collection).PropertyChanged += (sender, e) =>
			{
				propertyChanged = true;
				changedProps.Add(e.PropertyName);
			};

			collection.CollectionChanged += (sender, e) =>
			{
				args = e;
			};

			collection.Clear();

			Assert.IsTrue(propertyChanged, "CLEAR_1");
			Assert.IsTrue(changedProps.Contains("Count"), "CLEAR_2");
			Assert.IsTrue(changedProps.Contains("Item[]"), "CLEAR_3");

			CollectionChangedEventValidators.ValidateResetOperation(args, "CLEAR_4");
		}
	}

	internal static class CollectionChangedEventValidators
	{
		#region Validators

		internal static void AssertEquivalentLists(IList expected, IList actual, string message)
		{
			if (expected == null)
			{
				Assert.IsNull(actual, "LISTEQ_1A::" + message);
				return;
			}
			else
				Assert.IsNotNull(actual, "LISTEQ_1B::" + message);

			Assert.AreEqual(expected.Count, actual.Count, "LISTEQ_2::" + message);

			for (int i = 0; i < expected.Count; i++)
				Assert.AreEqual(expected[i], actual[i], "LISTEQ_3::" + message);
		}

		private static void ValidateCommon(NotifyCollectionChangedEventArgs args, NotifyCollectionChangedAction action, IList newItems, IList oldItems, int newIndex, int oldIndex, string message)
		{
			Assert.IsNotNull(args, "NCCVAL_1::" + message);

			Assert.AreEqual(action, args.Action, "NCCVAL_2::" + message);

			AssertEquivalentLists(newItems, args.NewItems, "NCCVAL_3::" + message);
			AssertEquivalentLists(oldItems, args.OldItems, "NCCVAL_4::" + message);

			Assert.AreEqual(newIndex, args.NewStartingIndex, "NCCVAL_5::" + message);
			Assert.AreEqual(oldIndex, args.OldStartingIndex, "NCCVAL_6::" + message);
		}

		internal static void ValidateResetOperation(NotifyCollectionChangedEventArgs args, string message)
		{
			ValidateCommon(args, NotifyCollectionChangedAction.Reset, null, null, -1, -1, message);
		}

		internal static void ValidateAddOperation(NotifyCollectionChangedEventArgs args, IList newItems, string message)
		{
			ValidateAddOperation(args, newItems, -1, message);
		}

		internal static void ValidateAddOperation(NotifyCollectionChangedEventArgs args, IList newItems, int startIndex, string message)
		{
			ValidateCommon(args, NotifyCollectionChangedAction.Add, newItems, null, startIndex, -1, message);
		}

		internal static void ValidateRemoveOperation(NotifyCollectionChangedEventArgs args, IList oldItems, string message)
		{
			ValidateRemoveOperation(args, oldItems, -1, message);
		}

		internal static void ValidateRemoveOperation(NotifyCollectionChangedEventArgs args, IList oldItems, int startIndex, string message)
		{
			ValidateCommon(args, NotifyCollectionChangedAction.Remove, null, oldItems, -1, startIndex, message);
		}

		internal static void ValidateReplaceOperation(NotifyCollectionChangedEventArgs args, IList oldItems, IList newItems, string message)
		{
			ValidateReplaceOperation(args, oldItems, newItems, -1, message);
		}

		internal static void ValidateReplaceOperation(NotifyCollectionChangedEventArgs args, IList oldItems, IList newItems, int startIndex, string message)
		{
			ValidateCommon(args, NotifyCollectionChangedAction.Replace, newItems, oldItems, startIndex, startIndex, message);
		}

		internal static void ValidateMoveOperation(NotifyCollectionChangedEventArgs args, IList changedItems, int newIndex, int oldIndex, string message)
		{
			ValidateCommon(args, NotifyCollectionChangedAction.Move, changedItems, changedItems, newIndex, oldIndex, message);
		}

		#endregion
	}
}
