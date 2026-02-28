using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AndanteTribe.Unity.Extensions.Tests
{
    [Ignore("This is a dummy class for testing purposes. It should not be included in the test suite.")]
    public class DummyAddressData : ScriptableObject
    {
        [SerializeField]
        private AssetReferenceT<GameObject> _prefabReference;
        
        [SerializeField]
        private AssetReferenceT<Material> _materialReference;

        public AssetReferenceT<GameObject> PrefabReference => _prefabReference;
        public string PrefabAddress => _prefabReference.RuntimeKey.ToString();
        public AssetReferenceT<Material> MaterialReference => _materialReference;
        public string MaterialAddress => _materialReference.RuntimeKey.ToString();

        public static DummyAddressData Load()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DummyAddressData>("Packages/jp.andantetribe.assetsregistry/Tests/Runtime/DummyData.asset");
        }
    }
}