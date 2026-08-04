#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace AndanteTribe.Unity.Extensions
{
    /// <summary>
    /// A registry that caches handles of loaded assets.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using System.Threading;
    /// using AndanteTribe.Unity.Extensions;
    /// using Cysharp.Threading.Tasks;
    /// using UnityEngine;
    ///
    /// public class AssetsRegistrySample : MonoBehaviour
    /// {
    ///     private readonly AssetsRegistry _registry = new AssetsRegistry();
    ///
    ///     private async UniTaskVoid Start()
    ///     {
    ///         // 1. Example of using LoadAsync to directly load a prefab and Instantiate it
    ///         var prefab = await _registry.LoadAsync<GameObject>("assets/prefabs/MyPrefab.prefab", destroyCancellationToken);
    ///         Instantiate(prefab, Vector3.zero, Quaternion.identity);
    ///
    ///         // 2. Example of using InstantiateAsync to load and instantiate a specific component from a prefab
    ///         //    (assumes that MyComponent is attached to the prefab)
    ///         var component = await _registry.InstantiateAsync<MyComponent>("assets/prefabs/MyPrefabWithComponent.prefab", transform, destroyCancellationToken);
    ///         component.transform.localPosition = Vector3.up;
    ///     }
    ///
    ///     private void OnDestroy()
    ///     {
    ///         _registry.Dispose();
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class AssetsRegistry : IDisposable
    {
        private readonly Dictionary<AsyncOperationHandle, int> _handleReferenceCounts = new(AsyncOperationHandleEqualityComparer.Default);

        /// <summary>
        /// The number of Addressables handles owned by this registry.
        /// </summary>
        public int Count => _handleReferenceCounts.Count;

        /// <summary>
        /// Load an asset asynchronously.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public async UniTask<TObject> LoadAsync<TObject>(string address, CancellationToken cancellationToken) where TObject : Object
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.LoadAssetAsync<TObject>(address);
            try
            {
                var result = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (!_handleReferenceCounts.TryAdd(handle, 1))
                {
                    _handleReferenceCounts[handle]++;
                }
                return result;
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }

        /// <summary>
        /// Load an asset asynchronously.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public async UniTask<TObject> LoadAsync<TObject>(AssetReferenceT<TObject> reference, CancellationToken cancellationToken) where TObject : Object
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.LoadAssetAsync<TObject>(reference);
            try
            {
                var result = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (!_handleReferenceCounts.TryAdd(handle, 1))
                {
                    _handleReferenceCounts[handle]++;
                }
                return result;
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }

        /// <summary>
        /// Load an asset, instantiate it, and return the specified component.
        /// </summary>
        /// <remarks>
        /// For prefab instantiation.
        /// </remarks>
        /// <param name="address"></param>
        /// <param name="parent"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public async UniTask<TComponent> InstantiateAsync<TComponent>(
            string address, Transform parent, CancellationToken cancellationToken) where TComponent : Component
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            try
            {
                var obj = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (!obj.TryGetComponent<TComponent>(out var component))
                {
                    throw new InvalidOperationException($"Could not retrieve the specified type {typeof(TComponent)} from {handle.DebugName}.");
                }

                var result = await Object.InstantiateAsync(component, parent).WithCancellation(cancellationToken);
                var instance = result[0];
                if (!_handleReferenceCounts.TryAdd(handle, 1))
                {
                    _handleReferenceCounts[handle]++;
                }
                return instance;
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }

        /// <summary>
        /// Load an asset, instantiate it, and return the specified component.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="parent"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async UniTask<TComponent> InstantiateAsync<TComponent>(
            AssetReferenceT<GameObject> reference, Transform parent, CancellationToken cancellationToken) where TComponent : Component
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.LoadAssetAsync<GameObject>(reference);
            try
            {
                var obj = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (!obj.TryGetComponent<TComponent>(out var component))
                {
                    throw new InvalidOperationException($"Could not retrieve the specified type {typeof(TComponent)} from {handle.DebugName}.");
                }

                var result = await Object.InstantiateAsync(component, parent).WithCancellation(cancellationToken);
                var instance = result[0];
                if (!_handleReferenceCounts.TryAdd(handle, 1))
                {
                    _handleReferenceCounts[handle]++;
                }
                return instance;
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }

        /// <summary>
        /// Unload all cached assets.
        /// </summary>
        public void Clear()
        {
            foreach (var (handle, value) in _handleReferenceCounts)
            {
                for (var i = 0; i < value && handle.IsValid(); i++)
                {
                    Addressables.Release(handle);
                }
            }
            _handleReferenceCounts.Clear();
        }

        /// <inheritdoc/>
        public void Dispose() => Clear();
    }
}
