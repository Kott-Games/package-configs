using System;
using UnityEngine;

namespace Kott.Configs
{
    [Serializable]
    public class CreateDataWrapper<T>
    {
        [SerializeField] private T _value;
    }
}
