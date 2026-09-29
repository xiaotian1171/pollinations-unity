using System;
using System.Collections.Generic;

namespace Pollinations.Tests
{
    /// <summary>A tiny assertion harness: no test framework to install.</summary>
    public static class Check
    {
        private static readonly List<string> _failures = new List<string>();
        private static int _passed;
        private static string _suite = "";

        public static void Suite(string name)
        {
            _suite = name;
            Console.WriteLine("-- " + name);
        }

        public static bool Equal(object expected, object actual, string what)
        {
            bool ok = Same(expected, actual);
            return Record(ok, what, ok ? "" : "expected <" + Show(expected) + "> but was <" + Show(actual) + ">");
        }

        private static bool Same(object expected, object actual)
        {
            if (expected == null || actual == null)
            {
                return Equals(expected, actual);
            }

            var expectedList = expected as System.Collections.IEnumerable;
            var actualList = actual as System.Collections.IEnumerable;
            if (expectedList != null && !(expected is string) && actualList != null)
            {
                var left = new List<object>();
                var right = new List<object>();
                foreach (object item in expectedList)
                {
                    left.Add(item);
                }

                foreach (object item in actualList)
                {
                    right.Add(item);
                }

                if (left.Count != right.Count)
                {
                    return false;
                }

                for (int i = 0; i < left.Count; i++)
                {
                    if (!Same(left[i], right[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return expected.Equals(actual);
        }

        public static bool True(bool value, string what)
        {
            return Record(value, what, value ? "" : "expected true");
        }

        public static bool False(bool value, string what)
        {
            return Record(!value, what, value ? "expected false" : "");
        }

        public static bool NotNull(object value, string what)
        {
            return Record(value != null, what, value == null ? "expected a value" : "");
        }

        public static bool Null(object value, string what)
        {
            return Record(value == null, what, value == null ? "" : "expected null but was <" + Show(value) + ">");
        }

        public static bool Contains(string haystack, string needle, string what)
        {
            bool ok = haystack != null && needle != null && haystack.Contains(needle);
            return Record(ok, what, ok ? "" : "<" + haystack + "> does not contain <" + needle + ">");
        }

        public static bool Throws(Action action, string what)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                _passed++;
                return true;
            }

            return Record(false, what, "expected an exception");
        }

        private static bool Record(bool ok, string what, string detail)
        {
            if (ok)
            {
                _passed++;
            }
            else
            {
                _failures.Add(_suite + ": " + what + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")"));
                Console.WriteLine("   FAIL " + what + " " + detail);
            }

            return ok;
        }

        private static string Show(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var text = value as string;
            if (text != null)
            {
                return text;
            }

            return value.ToString();
        }

        public static int Passed
        {
            get { return _passed; }
        }

        public static List<string> Failures
        {
            get { return _failures; }
        }

        public static int Report()
        {
            Console.WriteLine();
            Console.WriteLine("total: " + _passed + " passed, " + _failures.Count + " failed");
            if (_failures.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("failures:");
                foreach (string failure in _failures)
                {
                    Console.WriteLine("  - " + failure);
                }
            }

            return _failures.Count == 0 ? 0 : 1;
        }
    }
}
