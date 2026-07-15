using System;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Minimal hand-rolled assertion helpers — no NuGet needed, compiles on net40.
    /// Any failure throws with a clear message.
    /// </summary>
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
                Fail("Expected TRUE. " + (message ?? ""));
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
                Fail("Expected FALSE. " + (message ?? ""));
        }

        public static void AreEqual(string expected, string actual, string message = null)
        {
            if (expected != actual)
                Fail(string.Format("Expected:\n  [{0}]\nActual:\n  [{1}]\n{2}", expected, actual, message ?? ""));
        }

        public static void Contains(string haystack, string needle, string message = null)
        {
            if (haystack == null || !haystack.Contains(needle))
                Fail(string.Format("Expected to find [{0}] in:\n  [{1}]\n{2}", needle, haystack, message ?? ""));
        }

        public static void DoesNotContain(string haystack, string needle, string message = null)
        {
            if (haystack != null && haystack.Contains(needle))
                Fail(string.Format("Did NOT expect to find [{0}] in:\n  [{1}]\n{2}", needle, haystack, message ?? ""));
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
                Fail("Expected non-null. " + (message ?? ""));
        }

        public static void IsNotNullOrEmpty(string s, string message = null)
        {
            if (string.IsNullOrEmpty(s))
                Fail("Expected non-null/non-empty string. " + (message ?? ""));
        }

        public static void Throws<T>(Action action, string message = null) where T : Exception
        {
            try
            {
                action();
                Fail(string.Format("Expected {0} to be thrown but no exception was raised. {1}",
                    typeof(T).Name, message ?? ""));
            }
            catch (T) { /* expected */ }
            catch (Exception ex)
            {
                Fail(string.Format("Expected {0} but got {1}: {2}. {3}",
                    typeof(T).Name, ex.GetType().Name, ex.Message, message ?? ""));
            }
        }

        private static void Fail(string message)
        {
            throw new Exception("ASSERT FAILED: " + message);
        }
    }
}
