// Generated from LinqToCollections.ecs by LeMP custom tool. LeMP version: 2.3.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>
	/// This class enhances LINQ-to-Objects with extension methods that preserve the
	/// interface (e.g. Take(IList&lt;int>) returns a struct that implements IList&lt;int>)
	/// or have higher performance than the ones in System.Linq.Enumerable.
	/// </summary><remarks>
	/// For example, the <see cref="Enumerable.Last()"/> extension 
	/// method scans the entire list before returning the last item, while 
	/// <see cref="Last(IReadOnlyList{T})"/> and <see cref="Last(IList{T})"/> simply
	/// return the last item directly.
	/// </remarks>
	public static class LinqToCollections
	{
		// *** Reminder: do not edit the generated output! ***
		public static int Count<T>(this IList<T> list)
		{
			return list.Count;
		}
		// *** Reminder: do not edit the generated output! ***
		public static int Count<T>(this IReadOnlyCollection<T> list)
		{
			return list.Count;
		}
		// *** Reminder: do not edit the generated output! ***
		public static int Count<T>(this INegListSource<T> list)
		{
			return list.Count;
		}
		public static T FirstOrDefault<T>(this IList<T> list, T defaultValue = default(T))
		{
			if (list.Count > 0)
				return list[0];
			return defaultValue;
		}
		public static T FirstOrDefault<T>(this IListSource<T> list)
		{
			bool _;
			return list.TryGet(0, out _);
		}
		public static T FirstOrDefault<T>(this IListSource<T> list, T defaultValue)
		{
			bool fail;
			var result = list.TryGet(0, out fail);
			if (fail)
				return defaultValue;
			return result;
		}
		public static T FirstOrDefault<T>(this IListAndListSource<T> list, T defaultValue = default(T))
		{
			return FirstOrDefault((IListSource<T>) list, defaultValue);
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Gets the last item from the list (at <c>list.Max</c>).</summary>
		/// <exception cref="EmptySequenceException">The list is empty</exception>
		public static T Last<T>(this IList<T> list)
		{
			int last = list.Count - 1;
			if (last < 0)
				throw new EmptySequenceException();
			return list[last];
		}
		/// <summary>Gets the last item from the list (at <c>list.Max</c>), or <c>defaultValue</c> if the list is empty.</summary>
		public static T LastOrDefault<T>(this IList<T> list, T defaultValue = default(T))
		{
			int last = list.Count - 1;
			return last < 0 ? defaultValue : list[last];
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Gets the last item from the list (at <c>list.Max</c>).</summary>
		/// <exception cref="EmptySequenceException">The list is empty</exception>
		public static T Last<T>(this IReadOnlyList<T> list)
		{
			int last = list.Count - 1;
			if (last < 0)
				throw new EmptySequenceException();
			return list[last];
		}
		/// <summary>Gets the last item from the list (at <c>list.Max</c>), or <c>defaultValue</c> if the list is empty.</summary>
		public static T LastOrDefault<T>(this IReadOnlyList<T> list, T defaultValue = default(T))
		{
			int last = list.Count - 1;
			return last < 0 ? defaultValue : list[last];
		}
		// *** Reminder: do not edit the generated output! ***
		public static T Last<T>(this IListAndListSource<T> list) { return Last((IList<T>) list); }
		public static T LastOrDefault<T>(this IListAndListSource<T> list, T defaultValue = default(T)) {
			return LastOrDefault((IList<T>) list, defaultValue);
		}
	
		/// <summary>Gets the last item from the list (at <c>list.Max</c>).</summary>
		/// <exception cref="EmptySequenceException">The list is empty</exception>
		public static T Last<T>(this INegListSource<T> list)
		{
			int last = list.Max;
			if (last < list.Min)
				throw new EmptySequenceException();
			return list[last];
		}
		/// <summary>Gets the last item from the list (at <c>list.Max</c>), or <c>defaultValue</c> if the list is empty.</summary>
		public static T LastOrDefault<T>(this INegListSource<T> list, T defaultValue = default(T))
		{
			int last = list.Max;
			return last < list.Min ? defaultValue : list[last];
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Skips the specified number of elements immediately and 
		/// returns a slice of part of the list that remains, or an empty 
		/// slice if <c>start</c> is greater than or equal to the <c>list.Count</c>.</summary>
		public static Slice_<T> Skip<T>(this IListSource<T> list, int start)
		{
			return new Slice_<T>(list, start);
		}
		/// <summary>Returns a slice of the specified number of elements from 
		/// the beginning of the list, or a slice of the entire list if <c>count</c> 
		/// is greater than or equal to the <c>list.Count</c>.</summary>
		public static Slice_<T> Take<T>(this IListSource<T> list, int count)
		{
			return new Slice_<T>(list, 0, count);
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Skips the specified number of elements immediately and 
		/// returns a slice of part of the list that remains, or an empty 
		/// slice if <c>start</c> is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Skip<T>(this IList<T> list, int start)
		{
			return new ListSlice<T>(list, start);
		}
		/// <summary>Returns a slice of the specified number of elements from 
		/// the beginning of the list, or a slice of the entire list if <c>count</c> 
		/// is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Take<T>(this IList<T> list, int count)
		{
			return new ListSlice<T>(list, 0, count);
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Skips the specified number of elements immediately and 
		/// returns a slice of part of the list that remains, or an empty 
		/// slice if <c>start</c> is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Skip<T>(this IListAndListSource<T> list, int start)
		{
			return new ListSlice<T>(list, start);
		}
		/// <summary>Returns a slice of the specified number of elements from 
		/// the beginning of the list, or a slice of the entire list if <c>count</c> 
		/// is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Take<T>(this IListAndListSource<T> list, int count)
		{
			return new ListSlice<T>(list, 0, count);
		}
		// *** Reminder: do not edit the generated output! ***
		public static NegListSlice<T> Skip<T>(this INegListSource<T> list, int count)
		{
			CheckParam.IsNotNegative("count", count);
			return new NegListSlice<T>(list, checked(list.Min + count), int.MaxValue);
		}
		public static NegListSlice<T> Take<T>(this INegListSource<T> list, int count)
		{
			CheckParam.IsNotNegative("count", count);
			return new NegListSlice<T>(list, list.Min, count);
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Returns a slice of the initial elements of the list that meet the provided criteria. 
		/// The word "now" is added to the name because unlike Enumerable.TakeWhile, this method scans 
		/// the list immediately.</summary>
		/// <remarks>Example: new[] { 3, 6, 9, 12, 1, 2 }.TakeNowWhile(n => n < 10) returns a slice 
		/// (not a copy) of the first 3 elements.</remarks>
		public static ListSlice<T> TakeNowWhile<T>(this IList<T> list, Func<T,bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new ListSlice<T>(list);
				else if (!predicate(value.Value))
					return new ListSlice<T>(list, 0, i);
			}
		}
		/// <summary>Returns a slice without the initial elements of the list that meet the specified
		/// criteria. The word "now" is added to the name because unlike Enumerable.SkipWhile, this 
		/// method scans the list immediately.</summary>
		/// <remarks>Example: new[] { 4, 8, 12, 2, 10 }.SkipNowWhile(n => n < 10) returns a slice 
		/// (not a copy) of the last 2 elements.</remarks>
		public static ListSlice<T> SkipNowWhile<T>(this IList<T> list, Func<T,bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new ListSlice<T>();
				else if (!predicate(value.Value))
					return new ListSlice<T>(list, i);
			}
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Returns a slice of the initial elements of the list that meet the provided criteria. 
		/// The word "now" is added to the name because unlike Enumerable.TakeWhile, this method scans 
		/// the list immediately.</summary>
		/// <remarks>Example: new[] { 3, 6, 9, 12, 1, 2 }.TakeNowWhile(n => n < 10) returns a slice 
		/// (not a copy) of the first 3 elements.</remarks>
		public static Slice_<T> TakeNowWhile<T>(this IListSource<T> list, Func<T,bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new Slice_<T>(list);
				else if (!predicate(value.Value))
					return new Slice_<T>(list, 0, i);
			}
		}
		/// <summary>Returns a slice without the initial elements of the list that meet the specified
		/// criteria. The word "now" is added to the name because unlike Enumerable.SkipWhile, this 
		/// method scans the list immediately.</summary>
		/// <remarks>Example: new[] { 4, 8, 12, 2, 10 }.SkipNowWhile(n => n < 10) returns a slice 
		/// (not a copy) of the last 2 elements.</remarks>
		public static Slice_<T> SkipNowWhile<T>(this IListSource<T> list, Func<T,bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new Slice_<T>();
				else if (!predicate(value.Value))
					return new Slice_<T>(list, i);
			}
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Returns a slice of the initial elements of the list that meet the provided criteria. 
		/// The word "now" is added to the name because unlike Enumerable.TakeWhile, this method scans 
		/// the list immediately.</summary>
		/// <remarks>Example: new[] { 3, 6, 9, 12, 1, 2 }.TakeNowWhile(n => n < 10) returns a slice 
		/// (not a copy) of the first 3 elements.</remarks>
		public static NegListSlice<T> TakeNowWhile<T>(this INegListSource<T> list, Func<T,bool> predicate)
		{
			Maybe<T> value;
			for (int i = list.Min;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new NegListSlice<T>(list);
				else if (!predicate(value.Value))
					return new NegListSlice<T>(list, 0, i);
			}
		}
		/// <summary>Returns a slice without the initial elements of the list that meet the specified
		/// criteria. The word "now" is added to the name because unlike Enumerable.SkipWhile, this 
		/// method scans the list immediately.</summary>
		/// <remarks>Example: new[] { 4, 8, 12, 2, 10 }.SkipNowWhile(n => n < 10) returns a slice 
		/// (not a copy) of the last 2 elements.</remarks>
		public static NegListSlice<T> SkipNowWhile<T>(this INegListSource<T> list, Func<T,bool> predicate)
		{
			Maybe<T> value;
			for (int i = list.Min;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new NegListSlice<T>();
				else if (!predicate(value.Value))
					return new NegListSlice<T>(list, i);
			}
		}
		// *** Reminder: do not edit the generated output! ***
		public static ListSlice<T> TakeNowWhile<T>(this IListAndListSource<T> list, Func<T,bool> predicate)
		{
			return TakeNowWhile((IList<T>) list, predicate);
		}
		public static ListSlice<T> SkipNowWhile<T>(this IListAndListSource<T> list, Func<T,bool> predicate)
		{
			return SkipNowWhile((IList<T>) list, predicate);
		}
	
		/// <summary>Copies the contents of an IListSource or IReadOnlyList to an array.</summary>
		public static T[] ToArray<T>(this IReadOnlyList<T> c)
		{
			var array = new T[c.Count];
			for (int i = 0; i < array.Length; i++)
				array[i] = c[i];
			return array;
		}
	
		/// <summary>Copies the contents of an <see cref="INegListSource{T}"/> to an array.</summary>
		public static T[] ToArray<T>(this INegListSource<T> c)
		{
			var array = new T[c.Count];
			int min = c.Min;
			for (int i = 0; i < array.Length; i++)
				array[i] = c[i + min];
			return array;
		}
	
		// *** Reminder: do not edit the generated output! ***
		public static SelectListSource<T,TResult> Select<T,TResult>(this IListSource<T> source, Func<T,TResult> selector)
		{
			return new SelectListSource<T,TResult>(source, selector);
		}
		public static SelectListSource<T,TResult> Select<T,TResult>(this IListAndListSource<T> source, Func<T,TResult> selector)
		{
			return new SelectListSource<T,TResult>(source, selector);
		}
		public static SelectList<T,TResult> Select<T,TResult>(this IList<T> source, Func<T,TResult> selector)
		{
			return new SelectList<T,TResult>(source, selector);
		}
	
			// TODO:
			// public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source);
			// public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector);
			//     Projects each element of a sequence into a new form by incorporating the element's index.
			//   source:
			//     A sequence of values to invoke a transform function on.
			//   selector:
			//     A transform function to apply to each source element; the second parameter of
			//     the function represents the index of the source element.
			// public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector);
	}
}