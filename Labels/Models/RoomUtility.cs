using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labels.Models
{
    public static class RoomUtility
    {
        private static readonly Lookup _rooms = new Lookup()
        {
            { "cleanroom", "Clean Room" },
            { "wetchem", "ROBIN" },
            { "robin", "ROBIN" }
        };

        private static readonly Lookup _slugs = new Lookup()
        {
            { "cleanroom", "cleanroom" },
            { "wetchem", "wetchem" },
            { "robin", "wetchem" }
        };

        public static Lookup Rooms { get { return _rooms; } }

        public static Lookup Slugs { get { return _slugs; } }
    }

    public class Lookup : IEnumerable
    {
        private readonly Dictionary<string, string> _items;

        public Lookup()
        {
            _items = new Dictionary<string, string>();
        }

        public Lookup(Dictionary<string, string> items)
        {
            _items = items;
        }

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                    return null;

                if (ContainsKey(key))
                    return _items[key];
                else
                    return key;
            }
        }

        public void Add(string key, string value)
        {
            _items.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
}