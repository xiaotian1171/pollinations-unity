using System.Collections.Generic;

namespace Pollinations
{
    /// <summary>One entry of the live model catalogue.</summary>
    public sealed class PollinationsModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public List<string> Aliases { get; set; }
        public List<string> SupportedEndpoints { get; set; }

        public PollinationsModel()
        {
            Id = "";
            Title = "";
            Category = "";
            Publisher = "";
            Description = "";
            Aliases = new List<string>();
            SupportedEndpoints = new List<string>();
        }

        /// <summary>A label for a picker: the title when there is one, the id otherwise.</summary>
        public string Label
        {
            get { return string.IsNullOrEmpty(Title) ? Id : Title; }
        }

        public bool Supports(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                return true;
            }

            foreach (string supported in SupportedEndpoints)
            {
                if (supported == endpoint)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Parsing and filtering of the catalogue endpoints, which answer with a JSON array.</summary>
    public static class PollinationsModels
    {
        public static List<PollinationsModel> Parse(string json)
        {
            var models = new List<PollinationsModel>();
            object parsed;
            if (!PollinationsJson.TryParse(json, out parsed))
            {
                return models;
            }

            object entries = parsed;
            var envelope = parsed as Dictionary<string, object>;
            if (envelope != null)
            {
                entries = null;
                string[] keys = { "data", "models", "categories" };
                foreach (string key in keys)
                {
                    if (envelope.ContainsKey(key) && envelope[key] is List<object>)
                    {
                        entries = envelope[key];
                        break;
                    }
                }

                if (entries == null)
                {
                    return models;
                }
            }

            var list = entries as List<object>;
            if (list == null)
            {
                return models;
            }

            foreach (object item in list)
            {
                var entry = item as Dictionary<string, object>;
                if (entry == null)
                {
                    continue;
                }

                var model = new PollinationsModel
                {
                    Id = PollinationsJson.GetString(entry, "name"),
                    Title = PollinationsJson.GetString(entry, "title"),
                    Category = PollinationsJson.GetString(entry, "category"),
                    Publisher = PollinationsJson.GetString(entry, "publisher"),
                    Description = PollinationsJson.GetString(entry, "description"),
                };

                if (string.IsNullOrEmpty(model.Id))
                {
                    model.Id = PollinationsJson.GetString(entry, "id");
                }

                model.Aliases = Strings(entry, "aliases");
                model.SupportedEndpoints = Strings(entry, "supported_endpoints");
                if (model.Id.Length == 0)
                {
                    continue;
                }

                models.Add(model);
            }

            return models;
        }

        public static List<string> Ids(List<PollinationsModel> models)
        {
            var ids = new List<string>();
            foreach (PollinationsModel model in models)
            {
                ids.Add(model.Id);
            }

            return ids;
        }

        /// <summary>The models that accept a given endpoint, e.g. "/v1/chat/completions".</summary>
        public static List<PollinationsModel> Supporting(List<PollinationsModel> models, string endpoint)
        {
            var result = new List<PollinationsModel>();
            foreach (PollinationsModel model in models)
            {
                if (model.Supports(endpoint))
                {
                    result.Add(model);
                }
            }

            return result;
        }

        public static bool HasModel(List<PollinationsModel> models, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (PollinationsModel model in models)
            {
                if (model.Id == id)
                {
                    return true;
                }

                foreach (string alias in model.Aliases)
                {
                    if (alias == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static PollinationsModel Find(List<PollinationsModel> models, string id)
        {
            foreach (PollinationsModel model in models)
            {
                if (model.Id == id)
                {
                    return model;
                }

                foreach (string alias in model.Aliases)
                {
                    if (alias == id)
                    {
                        return model;
                    }
                }
            }

            return null;
        }

        private static List<string> Strings(Dictionary<string, object> entry, string key)
        {
            var result = new List<string>();
            if (!entry.ContainsKey(key))
            {
                return result;
            }

            var list = entry[key] as List<object>;
            if (list == null)
            {
                return result;
            }

            foreach (object item in list)
            {
                string text = PollinationsJson.AsString(item);
                if (!string.IsNullOrEmpty(text))
                {
                    result.Add(text);
                }
            }

            return result;
        }
    }
}
