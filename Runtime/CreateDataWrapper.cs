using System;
using UnityEngine;

namespace Kott.Configs
{
    [Serializable]
    public class CreateDataWrapper<T>
    {
        [SerializeReference] private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }
    }
}
