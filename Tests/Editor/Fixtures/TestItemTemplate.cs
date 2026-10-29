using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs.Tests
{
    public class TestItemTemplate : Template
    {
        public int Damage;
        public Vector3 Offset;
        public List<string> Tags = new();
        public TemplateReference<TestItemTemplate> Upgrade = new();
        public TemplateAssetReference<Sprite> Icon = new();
    }
}
