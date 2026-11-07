using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>One document of the knowledge base: an incident report, a procedure or a notice. The texts are data (Assets/_Project/Data/Lore).</summary>
    [Serializable]
    public class LoreArticle
    {
        public string id;

        /// <summary>Shown as a small tag: for example "Incident", "Procedure" or "Notice".</summary>
        public string category;

        public string title;
        public string date;
        public string text;
    }

    [Serializable]
    internal class LoreFile
    {
        public LoreArticle[] articles;
    }

    public static class LoreLibrary
    {
        /// <summary>Reads the lore file: <c>{ "articles": [ { "id", "category", "title", "date", "text" } ] }</c>. A broken file gives an empty list and a Console error.</summary>
        public static IReadOnlyList<LoreArticle> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<LoreArticle>();

            try
            {
                var file = JsonUtility.FromJson<LoreFile>(json);
                if (file?.articles == null) return new List<LoreArticle>();

                var articles = new List<LoreArticle>();
                foreach (var article in file.articles)
                {
                    if (article == null || string.IsNullOrWhiteSpace(article.title)) continue;
                    articles.Add(article);
                }
                return articles;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"The lore file is not valid JSON: {exception.Message}");
                return new List<LoreArticle>();
            }
        }
    }
}
