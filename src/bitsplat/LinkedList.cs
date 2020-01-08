using System;
using System.Collections;
using System.Collections.Generic;

namespace bitsplat
{
    public class LinkedList<T> : IEnumerable<T>
    {
        public bool IsClosed => _last?.Next == _first;
        public T Value => _current.Value;
        private LinkedListItem _current;
        private readonly LinkedListItem _first;
        private LinkedListItem _last;

        public LinkedList(T value)
        {
            _current = new LinkedListItem(value);
            _first = _current;
            _last = _current;
        }

        public void Close()
        {
            _last.Next = _first;
        }

        public T Step()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(
                    $"Cannot step through non-closed linked list"
                );
            }

            var value = _current.Value;
            _current = _current.Next;
            return value;
        }

        public LinkedList<T> Add(T value)
        {
            _last.Next = new LinkedListItem(value)
            {
                Previous = _last
            };
            _last = _last.Next;
            return this;
        }

        public class LinkedListItem
        {
            public LinkedListItem Next { get; set; }
            public LinkedListItem Previous { get; set; }
            public T Value { get; }

            public LinkedListItem(T value)
            {
                Value = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new LinkedListEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class LinkedListEnumerator
            : IEnumerator<T>
        {
            private readonly LinkedList<T> _list;
            private LinkedListItem _current;

            public LinkedListEnumerator(LinkedList<T> list)
            {
                _list = list;
                _current = new LinkedListItem(default(T))
                {
                    Next = _list._first
                };
            }

            public bool MoveNext()
            {
                if (_current.Next == null)
                {
                    return false;
                }

                _current = _current.Next;
                return true;
            }

            public void Reset()
            {
                _current = _list._first;
            }

            public T Current => _current.Value;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}