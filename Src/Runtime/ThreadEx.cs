using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Loyc.Runtime
{
	/// <summary>Creates and controls a thread, and fills in a gap in the
	/// .NET framework by propagating thread-local variables from parent
	/// to child threads, and by providing a ThreadStarting event.</summary>
	/// <remarks>
	/// This class is a decorator for the Thread class and thus a 
	/// drop-in replacement, except that only the most common methods and
	/// properties (both static and non-static) are provided.
	/// <para/>
	/// A child thread inherits a thread-local value from a parent thread
	/// only if ForkThread.AllocateDataSlot, ForkThread.AllocateNamedDataSlot
	/// or ForkThread.GetNamedDataSlot was called to create the variable.
	/// Sadly, there is no way to provide inheritance for variables marked by
	/// [ThreadStatic].
	/// <para/>
	/// TODO: rewrite ThreadState property for .NET compact framework.
	/// </remarks>
	public class ThreadEx
	{
		protected Thread _parent; // set by Start()
		protected Thread _thread; // underlying thread
		protected ThreadStart _ts1;
		protected ParameterizedThreadStart _ts2;
		protected int _startState = 0;
		
		protected internal static List<WeakReference<ThreadLocalVariableBase>> _TLVs = new List<WeakReference<ThreadLocalVariableBase>>();
		
		/// <summary>
		/// This event is called in the context of a newly-started thread, provided
		/// that the thread is started by the Start() method of this class (rather
		/// than Thread.Start()).
		/// </summary>
		/// <remarks>The Start() method blocks until this event completes.</remarks>
		public static event EventHandler<ThreadStartEventArgs> ThreadStarting;

		/// <summary>
		/// This event is called when a thread is stopping, if the thread is stopping
		/// gracefully and provided that it was started by the Start() method of this 
		/// class (rather than Thread.Start()).
		/// </summary>
		public static event EventHandler<ThreadStartEventArgs> ThreadStopping;

		public ThreadEx(ParameterizedThreadStart start)
			{ _thread = new Thread(ThreadStart); _ts2 = start; }
		public ThreadEx(ThreadStart start)
			{ _thread = new Thread(ThreadStart); _ts1 = start; }
		public ThreadEx(ParameterizedThreadStart start, int maxStackSize)
			{ _thread = new Thread(ThreadStart, maxStackSize); _ts2 = start; }
		public ThreadEx(ThreadStart start, int maxStackSize)
			{ _thread = new Thread(ThreadStart, maxStackSize); _ts1 = start; }		

		/// <summary>
		/// Causes the operating system to change the state of the current instance to
		/// System.Threading.ThreadState.Running.
		/// </summary>
		public void Start() { Start(null); }

		/// <summary>
		/// Causes the operating system to change the state of the current instance to
		/// System.Threading.ThreadState.Running. Start() does not return until the
		/// ThreadStarted event is handled.
		/// </summary><remarks>
		/// Once the thread terminates, it CANNOT be restarted with another call to Start.
		/// </remarks>
		public virtual void Start(object parameter)
		{
			if (Interlocked.CompareExchange(ref _startState, 1, 0) != 0)
				throw new ThreadStateException("The thread has already been started.");

			Debug.Assert(_parent == null);
			_parent = Thread.CurrentThread;

			_thread.Start(parameter);
				
			while(_startState == 1)
				Thread.Sleep(0);
		}

		protected virtual void ThreadStart(object parameter)
		{
			Debug.Assert(_thread == Thread.CurrentThread);

			try {
				// Inherit thread-local variables from parent
				for (int i = 0; i < _TLVs.Count; i++) {
					ThreadLocalVariableBase v = _TLVs[i].Target;
					if (v != null)
						v.Propagate(_parent.ManagedThreadId, _thread.ManagedThreadId);
				}

				// Note that Start() is still running in the parent thread
				if (ThreadStarting != null)
					ThreadStarting(this, new ThreadStartEventArgs(_parent, this));

				_startState = 2; // allow parent thread to continue

				if (_ts2 != null)
					_ts2(parameter);
				else
					_ts1();
			} finally {
				_startState = 3; // ensure parent thread continues
				
				// Inherit notify thread-local variables of termination
				for (int i = 0; i < _TLVs.Count; i++) {
					ThreadLocalVariableBase v = _TLVs[i].Target;
					if (v != null)
						v.Terminate(_thread.ManagedThreadId);
				}

				if (ThreadStopping != null)
					ThreadStopping(this, new ThreadStartEventArgs(_parent, this));
			}
		}

		/// <summary>
		/// Gets the currently running thread.
		/// </summary>
		public static Thread CurrentThread { get { return Thread.CurrentThread; } }
		/// <summary>
		/// Gets or sets a value indicating whether or not a thread is a background thread.
		/// </summary>
		public bool IsBackground { get { return _thread.IsBackground; } set { _thread.IsBackground = value; } }
		/// <summary>
		/// Gets a unique identifier for the current managed thread.
		/// </summary>
		public int ManagedThreadId { get { return _thread.ManagedThreadId; } }
		/// <summary>
		/// Gets or sets the name of the thread.
		/// </summary>
		public string Name { get { return _thread.Name; } set { _thread.Name = value; } }
		/// <summary>
		/// Gets or sets a value indicating the scheduling priority of a thread.
		/// </summary>
		public ThreadPriority Priority { get { return _thread.Priority; } set { _thread.Priority = value; } }
		/// <summary>
		/// Gets a value containing the states of the current thread.
		/// </summary>
		public System.Threading.ThreadState ThreadState { get { return _thread.ThreadState; } }
		/// <summary>
		/// Raises a System.Threading.ThreadAbortException in the thread on which it
		/// is invoked, to begin the process of terminating the thread while also providing
		/// exception information about the thread termination. Calling this method usually
		/// terminates the thread.
		/// </summary>
		public void Abort(object stateInfo) { _thread.Abort(stateInfo); }
		/// <summary>
		/// Returns the current domain in which the current thread is running.
		/// </summary>
		public static AppDomain GetDomain() { return Thread.GetDomain(); }
		/// <summary>
		/// Returns a hash code for the current thread.
		/// </summary>
		public override int GetHashCode() { return _thread.GetHashCode(); }
		/// <summary>
		/// Blocks the calling thread until a thread terminates, while continuing to
		/// perform standard COM and SendMessage pumping.
		/// </summary>
		public void Join() { _thread.Join(); }
		/// <summary>
		/// Blocks the calling thread until a thread terminates or the specified time 
		/// elapses, while continuing to perform standard COM and SendMessage pumping. 
		/// </summary>
		public bool Join(int milliseconds) { return _thread.Join(milliseconds); }
		/// <summary>
		/// Suspends the current thread for a specified time.
		/// </summary>
		public static void Sleep(int millisecondsTimeout) { Thread.Sleep(millisecondsTimeout); }

		public Thread Thread { get { return _thread; } }
		public Thread ParentThread { get { return _parent; } }

		public bool IsAlive { 
			get { 
				System.Threading.ThreadState t = ThreadState;
				return t != System.Threading.ThreadState.Stopped &&
				       t != System.Threading.ThreadState.Unstarted &&
				       t != System.Threading.ThreadState.Aborted;
			}
		}

		internal static void RegisterTLV(ThreadLocalVariableBase tlv)
		{
			lock(_TLVs) {
				for (int i = 0; i < _TLVs.Count; i++)
					if (!_TLVs[i].IsAlive) {
						_TLVs[i].Target = tlv;
						return;
					}
				_TLVs.Add(new WeakReference<ThreadLocalVariableBase>(tlv));
			}
		}
	}

	public class ThreadStartEventArgs : EventArgs
	{
		public ThreadStartEventArgs(Thread parent, ThreadEx child) 
			{ ParentThread = parent; ChildThread = child; }
		public Thread ParentThread;
		public ThreadEx ChildThread;
	}

	public class WeakReference<T> : System.WeakReference
	{
		public WeakReference(T target) : base(target) { }
		public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection) { }
		#if !WindowsCE && !SmartPhone && !PocketPC
		protected WeakReference(SerializationInfo info, StreamingContext context) : base(info, context) {}
		#endif
		public new T Target
		{
			get { return (T)base.Target; }
			set { base.Target = value; }
		}
	}

	/// <summary>
	/// A fast, tiny 4-byte lock to support multiple readers or a single writer.
	/// Designed for low-contention, high-performance scenarios where reading is 
	/// common and writing is rare.
	/// </summary>
	/// <remarks>
	/// Do not use the default constructor! Use TinyReaderWriterLock.New as the
	/// initial value of the lock.
	/// <para/>
	/// Recursive locking is not supported: the same lock cannot be acquired twice 
	/// for writing on the same thread, nor can a reader lock be acquired after 
	/// the writer lock was acquired on the same thread. If you make either of 
	/// these mistakes, the lock will throw an NotSupportedException.
	/// <para/>
	/// You also cannot acquire a read lock followed recursively by a write lock.
	/// Attempting to do so will self-deadlock the thread, bacause 
	/// TinyReaderWriterLock does not track the identity of each reader.
	/// <para/>
	/// However, multiple reader locks can be acquired on the same thread, just as
	/// multiple reader locks can be acquired by different threads.
	/// <para/>
	/// Make sure you call ExitRead() or ExitWrite() in a finally block! When 
	/// compiled in debug mode, TinyReaderWriterLock will make sure you don't mix
	/// up ExitRead() and ExitWrite().
	/// <para/>
	/// The range of Thread.CurrentThread.ManagedThreadId is undocumented. I have
	/// assumed they don't use IDs close to int.MinValue, so I use values near
	/// int.MinValue to indicate the number of readers holding the lock.
	/// </remarks>
	public struct TinyReaderWriterLock
	{
		public static readonly TinyReaderWriterLock New = new TinyReaderWriterLock { _user = NoUser };

		internal const int NoUser = int.MinValue;
		internal const int MaxReader = NoUser + 256;
		internal int _user;
		
		/// <summary>Acquires the lock to protect read access to a shared resource.</summary>
		public void EnterReadLock()
		{
			// Fast no-contention case that can probably be inlined
			if (Interlocked.CompareExchange(ref _user, NoUser + 1, NoUser) != NoUser)
				EnterReadLock2();
		}

		private void EnterReadLock2()
		{
			for (;;)
			{
				// Wait for the resource to become available
				int user;
				while ((user = _user) >= MaxReader)
				{
					if (user == Thread.CurrentThread.ManagedThreadId)
						throw new NotSupportedException("TinyReaderWriterLock does not support a reader and writer lock on the same thread");
					Thread.Sleep(0);
				}

				// Try to claim the resource for read access (increment _user)
				if (user == Interlocked.CompareExchange(ref _user, user + 1, user))
					break;
			}
		}

		/// <summary>Releases a read lock that was acquired with EnterRead().</summary>
		public void ExitReadLock()
		{
			Debug.Assert(_user > NoUser && _user <= MaxReader);
			Interlocked.Decrement(ref _user);
		}

		/// <summary>Acquires the lock to protect write access to a shared resource.</summary>
		public void EnterWriteLock()
		{
			EnterWriteLock(Thread.CurrentThread.ManagedThreadId);
		}

		/// <summary>Acquires the lock to protect write access to a shared resource.</summary>
		/// <param name="threadID">Reports the value of Thread.CurrentThread.ManagedThreadId</param>
		public void EnterWriteLock(int threadID)
		{
			// Fast no-contention case that can probably be inlined
			if (Interlocked.CompareExchange(ref _user, threadID, NoUser) != NoUser)
				EnterWriteLock2(threadID);
		}

		private void EnterWriteLock2(int threadID)
		{
			// Wait for the resource to become unused, and claim it
			while (Interlocked.CompareExchange(ref _user, threadID, NoUser) != NoUser)
			{
				if (_user == threadID)
					 throw new NotSupportedException("TinyReaderWriterLock does not support recursive write locks");
				Thread.Sleep(0);
			}
		}

		/// <summary>Releases a write lock that was acquired with EnterWrite().</summary>
		public void ExitWriteLock()
		{
			Debug.Assert(_user == Thread.CurrentThread.ManagedThreadId);
			_user = NoUser;
		}
	}


	public abstract class ThreadLocalVariableBase
	{
		internal abstract void Propagate(int parentThreadId, int childThreadId);
		internal abstract void Terminate(int threadId);
	}

	/// <summary>Provides access to a thread-local variable through a dictionary 
	/// that maps thread IDs to values.</summary>
	/// <typeparam name="T">Type of variable to wrap</typeparam>
	/// <remarks>
	/// ThreadLocalVariable implements thread-local variables using a dictionary 
	/// that maps thread IDs to values.
	/// <para/>
	/// Variables of this type should always be static and they should NOT be 
	/// marked with the [ThreadStatic] attribute.
	/// <para/>
	/// ThreadLocalVariable(of T) is less convenient than the [ThreadStatic]
	/// attribute, but ThreadLocalVariable works with ThreadEx to propagate the 
	/// value of the variable from parent threads to child threads, and you can
	/// install a propagator function to customize the way the variable is 
	/// copied (e.g. in case you need a deep copy).
	/// <para/>
	/// Despite my optimizations, ThreadLocalVariable is just over half as fast 
	/// as a ThreadStatic variable in CLR 2.0, in a test with no thread 
	/// contention. Access to the dictionary accounts for almost half of the 
	/// execution time; try-finally (needed in case of asyncronous exceptions) 
	/// blocks use up 11%; calling Thread.CurrentThread.ManagedThreadId takes 
	/// about 9%; and the rest, I presume, is used up by the TinyReaderWriterLock.
	/// </remarks>
	public class ThreadLocalVariable<T> : ThreadLocalVariableBase
	{
		public delegate TResult Func<TArg0, TResult>(TArg0 arg0);

		protected Dictionary<int, T> _tls = new Dictionary<int,T>(5);
		protected TinyReaderWriterLock _lock = TinyReaderWriterLock.New;
		protected Func<T,T> _propagator = delegate(T v) { return v; };

		public ThreadLocalVariable()
		{
			ThreadEx.RegisterTLV(this);
		}

		/// <summary>Constructs a ThreadLocalVariable.</summary>
		/// <param name="initialValue">Initial value on the current thread. 
		/// Does not affect other threads that are already running.</param>
		public ThreadLocalVariable(T initialValue) 
			: this(initialValue, null) {}

		/// <summary>Constructs a ThreadLocalVariable.</summary>
		/// <param name="initialValue">Initial value on the current thread. 
		/// Does not affect other threads that are already running.</param>
		/// <param name="propagator">A function that copies (and possibly 
		/// modifies) the Value from a parent thread when starting a new 
		/// thread.</param>
		public ThreadLocalVariable(T initialValue, Func<T,T> propagator)
		{
			Value = initialValue;
			if (propagator != null)
				_propagator = propagator;
			ThreadEx.RegisterTLV(this);
		}

		internal override void Propagate(int parentThreadId, int childThreadId)
		{
			T value;

			_lock.EnterWriteLock();
			try {
				_tls.TryGetValue(parentThreadId, out value);
				_tls[childThreadId] = _propagator(value);
			} finally {
				_lock.ExitWriteLock();
			}
		}
		internal override void Terminate(int threadId)
		{
			_lock.EnterWriteLock();
			try {
				_tls.Remove(CurrentThreadId);
			} finally {
				_lock.ExitWriteLock();
			}
		}

		internal int CurrentThreadId 
		{
			get { return Thread.CurrentThread.ManagedThreadId; } 
		}

		public bool HasValue
		{
			get {
				_lock.EnterReadLock();
				try {
					return _tls.ContainsKey(CurrentThreadId);
				} finally {
					_lock.ExitReadLock();
				}
			}
		}

		public T Value { 
			get {
				_lock.EnterReadLock();
				T value;
				// Wrapping in a try-finally hurts performance by about 11% in a 
				// Release build. Even though TryGetValue doesn't throw, an 
				// asynchronous thread abort is theoretically possible :(
				try {
					_tls.TryGetValue(CurrentThreadId, out value);
				} finally {
					_lock.ExitReadLock();
				}
				return value;
			}
			set {
				int threadID = Thread.CurrentThread.ManagedThreadId;
				_lock.EnterWriteLock(threadID);
				try {
					_tls[threadID] = value;
				} finally {
					_lock.ExitWriteLock();
				}
			}
		}
	}

	[TestFixture]
	public class ThreadExTests
	{
		[Test]
		public void BasicChecks()
		{
			ThreadLocalVariable<int> threadVar = new ThreadLocalVariable<int>(123);
			Thread parent = Thread.CurrentThread;
			bool eventOccurred = false;
			bool valueOk = true, eventOk = true;
			bool stop = false;
			bool started = false;

			ThreadEx t = new ThreadEx(delegate(object o)
			{
				started = true;
				try
				{
					if ((int)o != 123 || threadVar.Value != 123)
						valueOk = false;
				}
				catch
				{
					valueOk = false;
				}
				while (!stop)
					GC.KeepAlive(""); // Waste time
				started = false;
			});

			EventHandler<ThreadStartEventArgs> eh = null;
			ThreadEx.ThreadStarting += (eh = delegate(object o, ThreadStartEventArgs e)
			{
				eventOccurred = true;
				if (e.ChildThread != t || e.ParentThread != parent)
					eventOk = false;
				ThreadEx.ThreadStarting -= eh;
			});

			Assert.IsFalse(t.IsAlive);
			Assert.AreEqual(System.Threading.ThreadState.Unstarted, t.ThreadState);
			t.Start(123);
			Assert.IsTrue(t.IsAlive);
			Assert.IsTrue(eventOccurred);
			Assert.IsTrue(eventOk);
			while(!started)
				ThreadEx.Sleep(0);
			Assert.AreEqual(System.Threading.ThreadState.Running, t.ThreadState);
			stop = true;
			Assert.IsTrue(t.Join(5000));
			Assert.IsTrue(valueOk);
			Assert.IsFalse(started);
		}
	}
}
