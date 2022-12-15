using System.Collections.Generic;

namespace Chomikuj.Rest
{
    public class Request
    {
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
        private readonly Dictionary<string, long> _longs = new Dictionary<string, long>();

        public Dictionary<string, string> StringParameters { get { return _strings; } }
        public Dictionary<string, bool> BoolParameters { get { return _bools; } }
        public Dictionary<string, int> IntParameters { get { return _ints; } }
        public Dictionary<string, long> LongParameters { get { return _longs; } } 
        public Dictionary<string, string> Headers { get; private set; }
        public string Url { get; private set; }

        public Request(string url)
        {
            Headers = new Dictionary<string, string>();
            Url = url;
        }

        public void AddParameter(string key, string value)
        {
            _strings.Add(key, value);
        }

        public void AddParameter(string key, bool value)
        {
            _bools.Add(key, value);
        }

        public void AddParameter(string key, int value)
        {
            _ints.Add(key, value);
        }

        public void AddParameter(string key, long value)
        {
            _longs.Add(key, value);
        }
    }
}