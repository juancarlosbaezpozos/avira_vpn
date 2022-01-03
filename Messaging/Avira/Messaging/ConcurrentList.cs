using System.Collections.Generic;

namespace Avira.Messaging
{
    public class ConcurrentList<T>
    {
        private List<T> writeList = new List<T>();

        private List<T> readList = new List<T>();

        private object modifyLock = new object();

        private bool dirty;

        public int Count => ReadList.Count;

        protected List<T> ReadList
        {
            get
            {
                if (dirty)
                {
                    lock (modifyLock)
                    {
                        readList = new List<T>(writeList);
                    }
                }

                return readList;
            }
        }

        public T this[int key]
        {
            get { return ReadList[key]; }
            set
            {
                lock (modifyLock)
                {
                    writeList[key] = value;
                    readList[key] = value;
                }
            }
        }

        public void Add(T value)
        {
            lock (modifyLock)
            {
                writeList.Add(value);
                dirty = true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ReadList.GetEnumerator();
        }

        public void Remove(T value)
        {
            lock (modifyLock)
            {
                writeList.Remove(value);
                dirty = true;
            }
        }

        public void Insert(int idx, T value)
        {
            lock (modifyLock)
            {
                writeList.Insert(idx, value);
                dirty = true;
            }
        }

        public void RemoveAt(int idx)
        {
            lock (modifyLock)
            {
                writeList.RemoveAt(idx);
                dirty = true;
            }
        }
    }
}