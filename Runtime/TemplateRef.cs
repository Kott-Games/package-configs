using System;
using UnityEngine;

namespace ToG.Config
{
    public interface ITemplateRef
    {
        Template Asset { get; set; }
    }

    [Serializable]
    public class TemplateRef<TTemplate> : ITemplateRef
        where TTemplate : Template
    {
        [SerializeField] private Template _asset;

        private TTemplate _template;

        public TTemplate T
        {
            get
            {
#if UNITY_EDITOR
                _template = (TTemplate)(object)_asset;
#else
                if (_template == null && _asset != null)
                    _template = (TTemplate)_asset;
#endif
                return _template;
            }
        }

        Template ITemplateRef.Asset
        {
            get
            {
                return _asset;
            }
            set
            {
                _asset = value;
                _template = (TTemplate)_asset;
            }
        }
    }
}
