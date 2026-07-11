using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace AndanteTribe.Unity.Extensions.Tests
{
    internal sealed class InMemoryAssetProvider : ResourceProviderBase
    {
        private readonly IReadOnlyDictionary<string, Object> _assets;

        public InMemoryAssetProvider(IReadOnlyDictionary<string, Object> assets) => _assets = assets;

        public Action? AfterProvide { get; set; }

        public override Type GetDefaultType(IResourceLocation location) => location.ResourceType;

        public override bool CanProvide(Type type, IResourceLocation location) => typeof(Object).IsAssignableFrom(type);

        public override void Provide(ProvideHandle provideHandle) => CompleteAsync(provideHandle).Forget();

        private async UniTaskVoid CompleteAsync(ProvideHandle provideHandle)
        {
            await UniTask.Yield();
            var key = provideHandle.Location.PrimaryKey;
            if (!_assets.TryGetValue(key, out var asset))
            {
                _assets.TryGetValue(provideHandle.Location.InternalId, out asset);
            }

            var exception = asset == null ? new InvalidOperationException($"No in-memory asset is registered for '{key}'.") : null;
            provideHandle.Complete(asset, asset != null, exception);

            var afterProvide = AfterProvide;
            AfterProvide = null;
            afterProvide?.Invoke();
        }

        public override void Release(IResourceLocation location, object asset)
        {
        }
    }
}