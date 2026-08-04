#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AndanteTribe.Unity.Extensions.Tests
{
    /// <summary>
    /// Play mode tests for AssetsRegistry
    /// </summary>
    public class AssetsRegistryTests
    {
        private const string PrefabAddress = "assets-registry-tests-prefab";
        private const string MaterialAddress = "assets-registry-tests-material";
        private const string MissingAssetAddress = "assets-registry-tests-missing";
        private const string PrefabGuid = "04cce3e98f1db5e408101d3af39d20e0";
        private const string MaterialGuid = "c1babf113d370ee468b258214dbf3b4d";

        private AssetsRegistry _registry = null!;
        private GameObject _prefab = null!;
        private Material _material = null!;
        private InMemoryAssetProvider _provider = null!;
        private ResourceLocationMap _locator = null!;
        private FieldInfo? _addressablesInstanceField;
        private object? _originalAddressablesInstance;

        private static AssetReferenceT<GameObject> PrefabReference => new(PrefabGuid);

        [SetUp]
        public void SetUp()
        {
            (_addressablesInstanceField, _originalAddressablesInstance) = PrepareAddressablesForDirectLocatorUse();
            _registry = new AssetsRegistry();
            _prefab = new GameObject("Cube");
            _prefab.AddComponent<BoxCollider>();

            var shader = Shader.Find("Hidden/InternalErrorShader");
            Assert.That(shader, Is.Not.Null);
            _material = new Material(shader!) { name = "Material" };

            _provider = new InMemoryAssetProvider(new Dictionary<string, Object>
            {
                [PrefabAddress] = _prefab,
                [PrefabGuid] = _prefab,
                [MaterialAddress] = _material,
                [MaterialGuid] = _material,
            });
            Addressables.ResourceManager.ResourceProviders.Add(_provider);

            _locator = new ResourceLocationMap("AssetsRegistryTests", capacity: 5);
            AddLocation(PrefabAddress, _prefab);
            AddLocation(PrefabGuid, _prefab);
            AddLocation(MaterialAddress, _material);
            AddLocation(MaterialGuid, _material);
            AddLocation(MissingAssetAddress, typeof(GameObject));
            Addressables.AddResourceLocator(_locator);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _registry.Dispose();
                Addressables.RemoveResourceLocator(_locator);
                Addressables.ResourceManager.ResourceProviders.Remove(_provider);
                CleanupGameObject(_prefab);
                Object.DestroyImmediate(_material);
            }
            finally
            {
                _addressablesInstanceField?.SetValue(null, _originalAddressablesInstance);
                _addressablesInstanceField = null;
                _originalAddressablesInstance = null;
            }
        }

        [UnityTest]
        public IEnumerator LoadAsyncWithStringAddressLoadsGameObject() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            var loadedObject = await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);

            // Assert
            Assert.That(loadedObject, Is.Not.Null);
            Assert.That(loadedObject.name, Is.EqualTo("Cube"));
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LoadAsyncWithStringAddressLoadsMaterial() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            var loadedMaterial = await _registry.LoadAsync<Material>(MaterialAddress, cts.Token);

            // Assert
            Assert.That(loadedMaterial, Is.Not.Null);
            Assert.That(loadedMaterial.name, Is.EqualTo("Material"));
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LoadAsyncWithAssetReferenceLoadsGameObject() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var assetReference = PrefabReference;

            // Act
            var loadedObject = await _registry.LoadAsync(assetReference, cts.Token);

            // Assert
            Assert.That(loadedObject, Is.Not.Null);
            Assert.That(loadedObject.name, Is.EqualTo("Cube"));
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithStringAddressInstantiatesWithComponent() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;

            try
            {
                // Act
                var instantiatedComponent = await _registry.InstantiateAsync<BoxCollider>(PrefabAddress, parent, cts.Token);

                // Assert
                Assert.That(instantiatedComponent, Is.Not.Null);
                Assert.That(instantiatedComponent.gameObject, Is.Not.Null);
                Assert.That(instantiatedComponent.transform.parent, Is.EqualTo(parent));
                Assert.That(_registry.Count, Is.EqualTo(1));

                // Cleanup instantiated object
                CleanupGameObject(instantiatedComponent.gameObject);
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithAssetReferenceInstantiatesWithComponent() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;
            var assetReference = PrefabReference;

            try
            {
                // Act
                var instantiatedComponent = await _registry.InstantiateAsync<BoxCollider>(assetReference, parent, cts.Token);

                // Assert
                Assert.That(instantiatedComponent, Is.Not.Null);
                Assert.That(instantiatedComponent.gameObject, Is.Not.Null);
                Assert.That(instantiatedComponent.transform.parent, Is.EqualTo(parent));
                Assert.That(_registry.Count, Is.EqualTo(1));

                // Cleanup instantiated object
                CleanupGameObject(instantiatedComponent.gameObject);
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithAssetReferenceAndMissingComponentThrowsInvalidOperationException() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;

            try
            {
                // Act & Assert
                var exceptionThrown = false;
                try
                {
                    await _registry.InstantiateAsync<Rigidbody>(PrefabReference, parent, cts.Token);
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                }

                Assert.That(exceptionThrown, Is.True, "Expected InvalidOperationException was not thrown");
                Assert.That(_registry.Count, Is.EqualTo(0));
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithMissingComponentThrowsInvalidOperationException() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;

            try
            {
                // Act & Assert
                // Cube prefab doesn't have a Rigidbody component
                var exceptionThrown = false;
                try
                {
                    await _registry.InstantiateAsync<Rigidbody>(PrefabAddress, parent, cts.Token);
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                }

                Assert.That(exceptionThrown, Is.True, "Expected InvalidOperationException was not thrown");

                // Verify handle was released
                Assert.That(_registry.Count, Is.EqualTo(0));
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator LoadAsyncMultipleCallsIncreasesCount() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            await _registry.LoadAsync<Material>(MaterialAddress, cts.Token);

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LoadAsyncWithCancellationThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel before calling LoadAsync

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            }
            catch (OperationCanceledException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True, "Expected OperationCanceledException was not thrown");
        });

        [UnityTest]
        public IEnumerator LoadAsyncWithCancellationReleasesHandle() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            try
            {
                await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - handle should be released after cancellation
            Assert.That(_registry.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator LoadAsyncWithProviderFailureReleasesHandle() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var destroyedOperationCount = _provider.DestroyedOperationCount;
            var originalExceptionHandler = ResourceManager.ExceptionHandler;
            ResourceManager.ExceptionHandler = (_, _) => { };

            try
            {
                // Act
                var exceptionThrown = false;
                try
                {
                    await _registry.LoadAsync<GameObject>(MissingAssetAddress, cts.Token);
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                }

                await UniTask.Yield();

                // Assert
                Assert.That(exceptionThrown, Is.True, "Expected InvalidOperationException was not thrown");
                Assert.That(_registry.Count, Is.EqualTo(0));
                Assert.That(_provider.DestroyedOperationCount, Is.EqualTo(destroyedOperationCount + 1));
            }
            finally
            {
                ResourceManager.ExceptionHandler = originalExceptionHandler;
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithCancellationThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;
            cts.Cancel(); // Cancel before calling InstantiateAsync

            try
            {
                // Act & Assert
                var exceptionThrown = false;
                try
                {
                    await _registry.InstantiateAsync<BoxCollider>(PrefabAddress, parent, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    exceptionThrown = true;
                }

                Assert.That(exceptionThrown, Is.True, "Expected OperationCanceledException was not thrown");
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithCancellationReleasesHandle() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;
            cts.Cancel();

            try
            {
                // Act
                try
                {
                    await _registry.InstantiateAsync<BoxCollider>(PrefabAddress, parent, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }

                // Assert - handle should be released after cancellation
                Assert.That(_registry.Count, Is.EqualTo(0));
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithStringAddressAndCancellationAfterLoadReleasesHandle() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;
            _provider.AfterProvide = cts.Cancel;

            try
            {
                // Act & Assert
                var exceptionThrown = false;
                try
                {
                    await _registry.InstantiateAsync<BoxCollider>(PrefabAddress, parent, cts.Token);
                }
                catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
                {
                    exceptionThrown = true;
                }

                Assert.That(exceptionThrown, Is.True, "Expected OperationCanceledException was not thrown");
                Assert.That(_registry.Count, Is.EqualTo(0));
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncWithAssetReferenceAndCancellationAfterLoadReleasesHandle() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;
            _provider.AfterProvide = cts.Cancel;

            try
            {
                // Act & Assert
                var exceptionThrown = false;
                try
                {
                    await _registry.InstantiateAsync<BoxCollider>(PrefabReference, parent, cts.Token);
                }
                catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
                {
                    exceptionThrown = true;
                }

                Assert.That(exceptionThrown, Is.True, "Expected OperationCanceledException was not thrown");
                Assert.That(_registry.Count, Is.EqualTo(0));
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator ClearReleasesAllHandles() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            await _registry.LoadAsync<Material>(MaterialAddress, cts.Token);
            Assert.That(_registry.Count, Is.EqualTo(2));

            // Act
            _registry.Clear();

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator DisposeReleasesAllHandles() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            await _registry.LoadAsync<Material>(MaterialAddress, cts.Token);
            Assert.That(_registry.Count, Is.EqualTo(2));

            // Act
            _registry.Dispose();

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator CountInitiallyZero() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
            await UniTask.Yield();
        });

        [UnityTest]
        public IEnumerator LoadAsyncSameAssetMultipleTimesReleasesAllReferences() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var destroyedOperationCount = _provider.DestroyedOperationCount;

            // Act
            var obj1 = await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            var obj2 = await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);

            // Assert
            Assert.That(obj1, Is.Not.Null);
            Assert.That(obj2, Is.Not.Null);
            Assert.That(obj1, Is.SameAs(obj2)); // Same asset instance
            Assert.That(_registry.Count, Is.EqualTo(1));

            // Act
            _registry.Clear();
            await UniTask.Yield();

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
            Assert.That(_provider.DestroyedOperationCount, Is.EqualTo(destroyedOperationCount + 1));
        });

        [UnityTest]
        public IEnumerator InstantiateAsyncCreatesIndependentInstances() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;

            try
            {
                // Act
                var instance1 = await _registry.InstantiateAsync<BoxCollider>(PrefabAddress, parent, cts.Token);
                var instance2 = await _registry.InstantiateAsync<BoxCollider>(PrefabAddress, parent, cts.Token);

                // Assert
                Assert.That(instance1, Is.Not.Null);
                Assert.That(instance2, Is.Not.Null);
                Assert.That(instance1, Is.Not.SameAs(instance2)); // Different instances
                Assert.That(instance1.gameObject, Is.Not.SameAs(instance2.gameObject));
                // Note: Addressables caches handles internally, so multiple loads of the same asset
                // return only one handle per Addressables loading session
                Assert.That(_registry.Count, Is.EqualTo(1));

                // Cleanup
                CleanupGameObject(instance1.gameObject);
                CleanupGameObject(instance2.gameObject);
            }
            finally
            {
                CleanupGameObject(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator ClearCanLoadAssetsAgainAfterClearing() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);
            _registry.Clear();

            // Act
            var loadedObject = await _registry.LoadAsync<GameObject>(PrefabAddress, cts.Token);

            // Assert
            Assert.IsNotNull(loadedObject);
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        private void AddLocation(string key, Object asset)
        {
            AddLocation(key, asset.GetType());
        }

        private void AddLocation(string key, Type resourceType)
        {
            _locator.Add(key, new ResourceLocationBase(key, key, _provider.ProviderId, resourceType));
        }

        private static (FieldInfo InstanceField, object? OriginalInstance) PrepareAddressablesForDirectLocatorUse()
        {
            var addressablesInstanceField = GetRequiredField(
                typeof(Addressables), "m_AddressablesInstance", BindingFlags.NonPublic | BindingFlags.Static);
            var originalAddressablesInstance = addressablesInstanceField.GetValue(null);
            var addressablesImplType = addressablesInstanceField.FieldType;
            var addressablesInstance = Activator.CreateInstance(addressablesImplType, new LRUCacheAllocationStrategy(1000, 1000, 100, 10));
            if (addressablesInstance == null)
            {
                throw new InvalidOperationException($"Could not create an instance of {addressablesImplType.FullName}.");
            }

            GetRequiredField(addressablesImplType, "hasStartedInitialization", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .SetValue(addressablesInstance, true);
            GetRequiredField(addressablesImplType, "m_InitializationOperation", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(addressablesInstance, default(AsyncOperationHandle<IResourceLocator>));
            GetRequiredField(addressablesImplType, "m_OnHandleCompleteAction", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(addressablesInstance, new Action<AsyncOperationHandle>(_ => { }));
            addressablesInstanceField.SetValue(null, addressablesInstance);

            return (addressablesInstanceField, originalAddressablesInstance);
        }

        private static FieldInfo GetRequiredField(Type type, string name, BindingFlags bindingFlags)
        {
            return type.GetField(name, bindingFlags) ?? throw new MissingFieldException(type.FullName, name);
        }

        private static void CleanupGameObject(GameObject obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
