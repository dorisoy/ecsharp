using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

// They want me to put a "where K: notnull" constraint on dictionaries, but I disagree:
// some Loyc dictionaries have never had that requirement. The warning says:
// type 'K' cannot be used as...'TKey' in...'IDictionary<TKey, TValue>'. Nullability of...'K' doesn't match 'notnull' constraint.
#pragma warning disable 8714

namespace Loyc.SyncLib
{
	/// <summary>Standard extension methods for <see cref="ISyncManager"/>.</summary>
	public static partial class SyncManagerExt
	{
		public static T? Sync<SM, T>(this SM sync,
			FieldId name, T? savable, SyncObjectFunc<SM, T> syncFunc,
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SM : ISyncManager
		{
			return ObjectSyncher.For(syncFunc, mode).Sync(ref sync, name, savable);
		}

		public static T? Sync<SM, SyncObj, T>(this SM sync,
			FieldId name, T? savable, SyncObj syncObj,
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
		{
			return new ObjectSyncher<SM, SyncObj, T>(syncObj, mode).Sync(ref sync, name, savable);
		}

		public static E SyncEnumAsString<SM, SyncObj, E>(this SM sync,
			FieldId name, E savable, 
			SubObjectMode mode = SubObjectMode.Deduplicate)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, E>
				where E : struct, Enum
			=> new SyncEnumAsString<SM, E>().Sync(ref sync, name, savable);

		#region SyncList/SyncColl/SyncDict/SyncMemory methods that accept SyncObjectFunc<SM, T>

		public static List<T>? SyncList<SM, T>(this SM sync,
			FieldId name, List<T>? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IList<T>? SyncList<SM, T>(this SM sync,
			FieldId name, IList<T>? savable, SyncObjectFunc<SM, T> syncItem, 
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, IList<T>, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength, Alloc<T>.List).Sync(ref sync, name, savable);

		public static T[]? SyncList<SM, T>(this SM sync,
			FieldId name, T[]? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<T>? SyncList<SM, T>(this SM sync,
			FieldId name, IReadOnlyList<T>? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IListSource<T>? SyncList<SM, T>(this SM sync,
			FieldId name, IListSource<T>? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyCollection<T>? SyncColl<SM, T>(this SM sync,
			FieldId name, IReadOnlyCollection<T>? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<T>? SyncColl<SM, T>(this SM sync,
			FieldId name, ICollection<T>? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ICollection<T>, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength, Alloc<T>.List).Sync(ref sync, name, savable);

		public static HashSet<T>? SyncColl<SM, T>(this SM sync,
			FieldId name, HashSet<T>? savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, HashSet<T>, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength, Alloc<T>.HashSet).Sync(ref sync, name, savable);

		public static Coll? SyncColl<SM, Coll, T>(this SM sync,
			FieldId name, Coll? savable, SyncObjectFunc<SM, T> syncItem, Func<int, Coll> alloc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where Coll : ICollection<T>
			=> new SyncList<SM, T, Coll, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength, alloc).Sync(ref sync, name, savable);

		public static Memory<T> SyncMemory<SM, T>(this SM sync,
			FieldId name, Memory<T> savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<T> SyncMemory<SM, T>(this SM sync,
			FieldId name, ReadOnlyMemory<T> savable, SyncObjectFunc<SM, T> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, T, ObjectSyncher<SM, AsISyncObject<SM, T>, T>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static Dictionary<K, V>? SyncDict<SM, K, V>(this SM sync,
			FieldId name, Dictionary<K, V>? savable, SyncObjectFunc<SM, KeyValuePair<K, V>> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, KeyValuePair<K,V>, Dictionary<K,V>, ObjectSyncher<SM, AsISyncObject<SM, KeyValuePair<K, V>>, KeyValuePair<K, V>>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength, min => new Dictionary<K, V>(min)).Sync(ref sync, name, savable);

		public static IDictionary<K, V>? SyncDict<SM, K, V>(this SM sync,
			FieldId name, IDictionary<K, V>? savable, SyncObjectFunc<SM, KeyValuePair<K, V>> syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
			=> new SyncList<SM, KeyValuePair<K,V>, IDictionary<K,V>, ObjectSyncher<SM, AsISyncObject<SM, KeyValuePair<K, V>>, KeyValuePair<K, V>>>
				(ObjectSyncher.For(syncItem, itemMode), listMode, tupleLength, min => new Dictionary<K, V>(min)).Sync(ref sync, name, savable);

		#endregion

		#region SyncList/SyncColl/SyncDict/SyncMemory methods that accept Symbol and ISyncField<SM, T>

		public static List<T>? SyncList<SM, T, SyncField>(this SM sync,
			FieldId name, List<T>? savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static IList<T>? SyncList<SM, T, SyncField>(this SM sync,
			FieldId name, IList<T>? savable, SyncField syncItem, 
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, IList<T>, SyncField>(syncItem, listMode, tupleLength, Alloc<T>.List).Sync(ref sync, name, savable);

		public static T[]? SyncList<SM, T, SyncField>(this SM sync,
			FieldId name, T[]? savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<T>? SyncList<SM, T, SyncField>(this SM sync,
			FieldId name, IReadOnlyList<T>? savable, SyncField syncItem, 
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static IListSource<T>? SyncList<SM, T, SyncField>(this SM sync,
			FieldId name, IListSource<T>? savable, SyncField syncItem, 
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyCollection<T>? SyncColl<SM, T, SyncField>(this SM sync,
			FieldId name, IReadOnlyCollection<T>? savable, SyncField syncItem, 
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<T>? SyncColl<SM, T, SyncField>(this SM sync,
			FieldId name, ICollection<T>? savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, ICollection<T>, SyncField>
				(syncItem, listMode, tupleLength, Alloc<T>.List).Sync(ref sync, name, savable);

		public static HashSet<T>? SyncColl<SM, T, SyncField>(this SM sync,
			FieldId name, HashSet<T>? savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, HashSet<T>, SyncField>(syncItem, listMode, tupleLength, Alloc<T>.HashSet).Sync(ref sync, name, savable);

		public static Coll? SyncColl<SM, Coll, T, SyncField>(this SM sync,
			FieldId name, Coll? savable, SyncField syncItem, Func<int, Coll> alloc,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where Coll : ICollection<T>
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, Coll, SyncField>(syncItem, listMode, tupleLength, alloc).Sync(ref sync, name, savable);

		public static Memory<T> SyncMemory<SM, T, SyncField>(this SM sync,
			FieldId name, Memory<T> savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<T> SyncMemory<SM, T, SyncField>(this SM sync,
			FieldId name, ReadOnlyMemory<T> savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, T>
			=> new SyncList<SM, T, SyncField>(syncItem, listMode, tupleLength).Sync(ref sync, name, savable);

		public static Dictionary<K, V>? SyncDict<SM, K, V, SyncField>(this SM sync,
			FieldId name, Dictionary<K, V>? savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, KeyValuePair<K,V>>
			=> new SyncList<SM, KeyValuePair<K,V>, Dictionary<K,V>, SyncField>
				(syncItem, listMode, tupleLength, min => new Dictionary<K, V>(min)).Sync(ref sync, name, savable);

		public static IDictionary<K, V>? SyncDict<SM, K, V, SyncField>(this SM sync,
			FieldId name, IDictionary<K, V>? savable, SyncField syncItem,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncField : ISyncField<SM, KeyValuePair<K,V>>
			=> new SyncList<SM, KeyValuePair<K,V>, IDictionary<K,V>, SyncField>
				(syncItem, listMode, tupleLength, min => new Dictionary<K, V>(min)).Sync(ref sync, name, savable);

		#endregion

		#region SyncList/SyncColl/SyncDict/SyncMemory methods that accept Symbol and ISyncObject<SM, T>

		public static List<T>? SyncList<SM, T, SyncObj>(this SM sync,
			FieldId name, List<T>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IList<T>? SyncList<SM, T, SyncObj>(this SM sync,
			FieldId name, IList<T>? savable, SyncObj syncItem, 
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, IList<T>, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength, Alloc<T>.List).Sync(ref sync, name, savable);

		public static T[]? SyncList<SM, T, SyncObj>(this SM sync,
			FieldId name, T[]? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyList<T>? SyncList<SM, T, SyncObj>(this SM sync,
			FieldId name, IReadOnlyList<T>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IListSource<T>? SyncList<SM, T, SyncObj>(this SM sync,
			FieldId name, IListSource<T>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static IReadOnlyCollection<T>? SyncColl<SM, T, SyncObj>(this SM sync,
			FieldId name, IReadOnlyCollection<T>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ICollection<T>? SyncColl<SM, T, SyncObj>(this SM sync,
			FieldId name, ICollection<T>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ICollection<T>, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength, Alloc<T>.List).Sync(ref sync, name, savable);

		public static HashSet<T>? SyncColl<SM, T, SyncObj>(this SM sync,
			FieldId name, HashSet<T>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, HashSet<T>, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength, Alloc<T>.HashSet).Sync(ref sync, name, savable);

		public static Coll? SyncColl<SM, Coll, T, SyncObj>(this SM sync,
			FieldId name, Coll? savable, SyncObj syncItem, Func<int, Coll> alloc,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where Coll : ICollection<T>
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, Coll, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength, alloc).Sync(ref sync, name, savable);

		public static Memory<T> SyncMemory<SM, T, SyncObj>(this SM sync,
			FieldId name, Memory<T> savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static ReadOnlyMemory<T> SyncMemory<SM, T, SyncObj>(this SM sync,
			FieldId name, ReadOnlyMemory<T> savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, T>
			=> new SyncList<SM, T, ObjectSyncher<SM, SyncObj, T>>
				(new ObjectSyncher<SM, SyncObj, T>(syncItem, itemMode), listMode, tupleLength).Sync(ref sync, name, savable);

		public static Dictionary<K, V>? SyncDict<SM, K, V, SyncObj>(this SM sync,
			FieldId name, Dictionary<K, V>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, KeyValuePair<K, V>>
			=> new SyncList<SM, KeyValuePair<K,V>, Dictionary<K,V>, ObjectSyncher<SM, SyncObj, KeyValuePair<K, V>>>
				(new ObjectSyncher<SM, SyncObj, KeyValuePair<K, V>>(syncItem, itemMode), listMode, tupleLength, min => new Dictionary<K, V>(min)).Sync(ref sync, name, savable);

		public static IDictionary<K, V>? SyncDict<SM, K, V, SyncObj>(this SM sync,
			FieldId name, IDictionary<K, V>? savable, SyncObj syncItem,
			SubObjectMode itemMode = SubObjectMode.Deduplicate,
			SubObjectMode listMode = SubObjectMode.List, int tupleLength = -1)
				where SM : ISyncManager
				where SyncObj : ISyncObject<SM, KeyValuePair<K, V>>
			=> new SyncList<SM, KeyValuePair<K,V>, IDictionary<K,V>, ObjectSyncher<SM, SyncObj, KeyValuePair<K, V>>>
				(new ObjectSyncher<SM, SyncObj, KeyValuePair<K, V>>(syncItem, itemMode), listMode, tupleLength, min => new Dictionary<K, V>(min)).Sync(ref sync, name, savable);

		#endregion
	}
}
