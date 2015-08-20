using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace WindowsApp.Common
{
    /// <summary>
    /// Implementation of IObservableMap that supports reentrancy for use as a default view
    /// model.
    /// </summary>
    public class ObservableDictionary : IObservableMap<string, object>
    {
        private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<string>
        {
            public ObservableDictionaryChangedEventArgs(CollectionChange change, string key)
            {
                this.CollectionChange = change;
                this.Key = key;
            }

            public CollectionChange CollectionChange { get; private set; }
            public string Key { get; private set; }
        }

        private Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        public event MapChangedEventHandler<string, object> MapChanged;

        private void InvokeMapChanged(CollectionChange change, string key)
        {
            var eventHandler = MapChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
            }
        }

        /// <summary>
        ///  TODO: Write Comment
        /// </summary>
        /// <param name="key">TODO: Write Comment</param>
        /// <param name="value"> TODO: Write Comment</param>
        public void Add(string key, object value)
        {
            this._dictionary.Add(key, value);
            this.InvokeMapChanged(CollectionChange.ItemInserted, key);
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="item">TODO: Write Comment</param>
        public void Add(KeyValuePair<string, object> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="key">TODO: Write Comment</param>
        /// <returns>TODO: Write Comment</returns>
        public bool Remove(string key)
        {
            if (this._dictionary.Remove(key))
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="item">TODO: Write Comment</param>
        /// <returns>TODO: Write Comment</returns>
        public bool Remove(KeyValuePair<string, object> item)
        {
            object currentValue;
            if (this._dictionary.TryGetValue(item.Key, out currentValue) &&
                Object.Equals(item.Value, currentValue) && this._dictionary.Remove(item.Key))
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="key">TODO: Write Comment</param>
        /// <returns>TODO: Write Comment</returns>
        public object this[string key]
        {
            get
            {
                return this._dictionary[key];
            }
            set
            {
                this._dictionary[key] = value;
                this.InvokeMapChanged(CollectionChange.ItemChanged, key);
            }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        public void Clear()
        {
            var priorKeys = this._dictionary.Keys.ToArray();
            this._dictionary.Clear();
            foreach (var key in priorKeys)
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
            }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        public ICollection<string> Keys
        {
            get { return this._dictionary.Keys; }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="key">TODO: Write Comment</param>
        /// <returns>TODO: Write Comment</returns>
        public bool ContainsKey(string key)
        {
            return this._dictionary.ContainsKey(key);
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="key">TODO: Write Comment</param>
        /// <param name="value">TODO: Write Comment</param>
        /// <returns></returns>
        public bool TryGetValue(string key, out object value)
        {
            return this._dictionary.TryGetValue(key, out value);
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        public ICollection<object> Values
        {
            get { return this._dictionary.Values; }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="item">TODO: Write Comment</param>
        /// <returns>TODO: Write Comment</returns>
        public bool Contains(KeyValuePair<string, object> item)
        {
            return this._dictionary.Contains(item);
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        public int Count
        {
            get { return this._dictionary.Count; }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <returns>TODO: Write Comment</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="array">TODO: Write Comment</param>
        /// <param name="arrayIndex">TODO: Write Comment</param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            int arraySize = array.Length;
            foreach (var pair in this._dictionary)
            {
                if (arrayIndex >= arraySize) break;
                array[arrayIndex++] = pair;
            }
        }
    }
}
