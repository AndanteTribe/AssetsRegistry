using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace AndanteTribe.Unity.Extensions.Tests
{
    internal sealed class InMemoryAssetProvider : ResourceProviderBase
    {
        private static readonly FieldInfo s_internalOperationField =
            typeof(ProvideHandle).GetField("m_InternalOp", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(ProvideHandle).FullName, "m_InternalOp");

        private readonly IReadOnlyDictionary<string, Object> _assets;

        public InMemoryAssetProvider(IReadOnlyDictionary<string, Object> assets) => _assets = assets;

        public Action? AfterProvide { get; set; }

        public int DestroyedOperationCount { get; private set; }

        public override Type GetDefaultType(IResourceLocation location) => location.ResourceType;

        public override bool CanProvide(Type type, IResourceLocation location) => typeof(Object).IsAssignableFrom(type);

        public override void Provide(ProvideHandle provideHandle)
        {
            var internalOperation = s_internalOperationField.GetValue(provideHandle);
            var operationHandle = (AsyncOperationHandle)Activator.CreateInstance(
                typeof(AsyncOperationHandle),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new[] { internalOperation },
                culture: null);
            operationHandle.Destroyed += _ => DestroyedOperationCount++;
            CompleteAsync(provideHandle).Forget();
        }

        private async UniTaskVoid CompleteAsync(ProvideHandle provideHandle)
        {
            await UniTask.Yield();
            var primaryKey = provideHandle.Location.PrimaryKey;
            var internalId = provideHandle.Location.InternalId;
            if (!_assets.TryGetValue(primaryKey, out var asset))
            {
                _assets.TryGetValue(internalId, out asset);
            }

            var exception = asset == null
                ? new InvalidOperationException($"No in-memory asset is registered for PrimaryKey '{primaryKey}' or InternalId '{internalId}'.")
                : null;
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
