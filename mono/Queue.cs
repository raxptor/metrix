using System;
using System.Collections.Generic;
using System.Text;

namespace Metrix
{
	public enum EventType
	{
		LOG,
		ANALYTICS
	};

	public class Queue
	{
		// Configures number of sinks.
		short StartRefCount;

		public struct Entry
		{
			public DateTime Time;
			public EventType Type;
			public string Data;
			public ulong Sequence;
			public short Refcount;
		}

		public struct Sink
		{
			public ulong Bookmark;
		}

		Entry[] _entries;
		ulong _front, _back, _end;

		public Queue()
		{
			_entries = new Entry[16];
			_end = ~0u;
		}

		void Expand()
		{

		}

		public void End()
		{
			_end = _front;
		}

		public void InsertLog(string Text)
		{
			lock (this)
			{
				if (StartRefCount == 0)
					return;
				if ((_front - _back) == (ulong)_entries.Length)
					Expand();
				int idx = (int)(_front % (ulong)_entries.Length);
				_entries[idx] = new Entry();
				_entries[idx].Sequence = _front;
				_entries[idx].Refcount = StartRefCount;
				_entries[idx].Type = EventType.LOG;
				_entries[idx].Time = DateTime.Now;
				_entries[idx].Data = Text;
				Console.WriteLine("[queue] - inserted [" + Text + "] " + _front);
				++_front;
			}
		}

		public void AddSink(ref Sink sink)
		{
			lock (this)
			{
				sink.Bookmark = _front;
				StartRefCount++;
			}
		}

		public bool Extract(ref Sink sink, Entry[] output, out int outCount)
		{
			if (sink.Bookmark == _end)
			{
				outCount = -1;
				return false;
			}
			outCount = 0;
			lock (this)
			{ 
				while (sink.Bookmark < _front && outCount < output.Length)
				{
					ulong where = sink.Bookmark++;
					int idx = (int)(where % (ulong)_entries.Length);
					output[outCount++] = _entries[idx];
					if (--_entries[idx].Refcount == 0)
					{
						if (where == _back)
							++_back;
					}
					// warn
					if (_entries[idx].Sequence != where)
						Console.WriteLine("Sequence corruption!");
				}
			}
			return outCount > 0;
		}
	}
}
