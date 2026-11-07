using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    [Serializable]
    internal class PasswordWordFile
    {
        public string[] words;
    }

    public static class PasswordWordList
    {
        /// <summary>Reads the word file: <c>{ "words": [ "SERVER", ... ] }</c>. A broken file gives an empty list and a Console error.</summary>
        public static IReadOnlyList<string> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();

            try
            {
                var file = JsonUtility.FromJson<PasswordWordFile>(json);
                return file?.words != null ? new List<string>(file.words) : new List<string>();
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"The password word file is not valid JSON: {exception.Message}");
                return new List<string>();
            }
        }
    }
}
