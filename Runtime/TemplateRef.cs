using System;
using UnityEngine;

namespace Kott.Configs
{
    public interface ITemplateRef
    {
        Template Asset { get; set; }
    }

    [Serializable]
    public class TemplateRef<T> : ITemplateRef
        where T : ITemplate
    {
        [SerializeField] private Template _asset;

        private T _template;

        public T Template
        {
            get
            {
#if UNITY_EDITOR
                _template = (T)(object)_asset;
#else
                if (_template == null && _asset != null)
                    _template = (T)(object)_asset;
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
                _template = (T)(object)_asset;
            }
        }
    }
}
