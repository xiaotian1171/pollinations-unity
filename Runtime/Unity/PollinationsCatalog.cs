using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Pollinations
{
    /// <summary>
    /// The live model catalogue, so a game offers whatever Pollinations serves today
    /// instead of hardcoding ids that go stale.
    /// </summary>
    [AddComponentMenu("Pollinations/Pollinations Catalog")]
    public class PollinationsCatalog : PollinationsBehaviour
    {
        [Serializable]
        public sealed class StringListEvent : UnityEvent<string>
        {
        }

        [Header("Catalogue")]
        [Tooltip("text, image, audio or embeddings.")]
        [SerializeField] private string modality = "text";

        [Tooltip("Only keep the models that accept this endpoint, e.g. /v1/chat/completions. Empty keeps all.")]
        [SerializeField] private string endpoint = "";

        [Tooltip("Called with each model id, so a dropdown can be filled.")]
        public StringListEvent onModel = new StringListEvent();

        public event Action<PollinationsResult> Loaded;

        /// <summary>Everything the endpoint returned.</summary>
        public List<PollinationsModel> Models { get; private set; }

        public PollinationsCatalog()
        {
            Models = new List<PollinationsModel>();
        }

        public string Modality
        {
            get { return modality; }
            set { modality = value; }
        }

        public string Endpoint
        {
            get { return endpoint; }
            set { endpoint = value; }
        }

        public async Task<PollinationsResult> FetchAsync(string modalityOverride = null)
        {
            string wanted = string.IsNullOrEmpty(modalityOverride) ? modality : modalityOverride;
            PollinationsResult result = await Client.GetAsync(PollinationsUrls.Models(wanted));
            Models = result.Ok ? PollinationsModels.Parse(result.Text) : new List<PollinationsModel>();

            if (result.Ok && Models.Count == 0)
            {
                result = PollinationsResult.Failure(
                    PollinationsErrorKind.Parse,
                    result.StatusCode,
                    result.Url,
                    "the catalogue could not be read");
            }

            Report(result);
            if (result.Ok)
            {
                foreach (PollinationsModel model in Usable())
                {
                    onModel.Invoke(model.Id);
                }

                if (Loaded != null)
                {
                    Loaded(result);
                }
            }

            return result;
        }

        /// <summary>The models that pass the endpoint filter.</summary>
        public List<PollinationsModel> Usable()
        {
            return PollinationsModels.Supporting(Models, endpoint);
        }

        public List<string> Ids()
        {
            return PollinationsModels.Ids(Usable());
        }

        public bool Has(string id)
        {
            return PollinationsModels.HasModel(Models, id);
        }
    }
}
