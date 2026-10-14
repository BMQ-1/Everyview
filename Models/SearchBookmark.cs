using System;

namespace EverythingModern.Models
{
    public class SearchBookmark
    {
        public string Title { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string IconSymbol { get; set; } = "Bookmark20";

        public SearchBookmark() { }

        public SearchBookmark(string title, string query, string description = "", string category = "General", string iconSymbol = "Bookmark20")
        {
            Title = title;
            Query = query;
            Description = description;
            Category = category;
            IconSymbol = iconSymbol;
        }
    }
}
