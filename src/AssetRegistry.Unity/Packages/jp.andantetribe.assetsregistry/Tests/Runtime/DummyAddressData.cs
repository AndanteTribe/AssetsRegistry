using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AndanteTribe.Unity.Extensions.Tests
{
    [Ignore("This is a dummy class for testing purposes. It should not be included in the test suite.")]
    public class DummyAddressData : ScriptableObject
    {
        [SerializeField]
        private AssetReferenceT<GameObject> prefabReference;
        
        [SerializeField]
        private AssetReferenceT<Material> materialReference;

        public AssetReferenceT<GameObject> PrefabReference => prefabReference;
        public string PrefabAddress => prefabReference.RuntimeKey.ToString();
        public AssetReferenceT<Material> MaterialReference => materialReference;
        public string MaterialAddress => materialReference.RuntimeKey.ToString();

        public static DummyAddressData Load()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DummyAddressData>("Packages/jp.andantetribe.assetsregistry/Tests/Runtime/DummyData.asset");
        }
    }
}