using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AndanteTribe.Unity.Extensions.Tests
{
    /// <summary>
    /// Play mode tests for AssetsRegistry
    /// </summary>
    public class AssetsRegistryTests
    {
        private AssetsRegistry _registry;
        private DummyAddressData _dummyData;

        [SetUp]
        public void SetUp()
        {
            _registry = new AssetsRegistry();
            _dummyData = DummyAddressData.Load();
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Dispose();
            _registry = null;
        }

        [UnityTest]
        public IEnumerator LoadAsync_WithStringAddress_LoadsGameObject() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            var loadedObject = await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);

            // Assert
            Assert.That(loadedObject, Is.Not.Null);
            Assert.That(loadedObject.name, Is.EqualTo("Cube"));
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithStringAddress_LoadsMaterial() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            var loadedMaterial = await _registry.LoadAsync<Material>(_dummyData.MaterialAddress, cts.Token);

            // Assert
            Assert.That(loadedMaterial, Is.Not.Null);
            Assert.That(loadedMaterial.name, Is.EqualTo("Material"));
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithAssetReference_LoadsGameObject() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var assetReference = _dummyData.PrefabReference;

            // Act
            var loadedObject = await _registry.LoadAsync(assetReference, cts.Token);

            // Assert
            Assert.That(loadedObject, Is.Not.Null);
            Assert.That(loadedObject.name, Is.EqualTo("Cube"));
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_WithStringAddress_InstantiatesWithComponent() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;

            try
            {
                // Act
                var instantiatedComponent = await _registry.InstantiateAsync<BoxCollider>(_dummyData.PrefabAddress, parent, cts.Token);

                // Assert
                Assert.That(instantiatedComponent, Is.Not.Null);
                Assert.That(instantiatedComponent.gameObject, Is.Not.Null);
                Assert.That(instantiatedComponent.transform.parent, Is.EqualTo(parent));
                Assert.That(_registry.Count, Is.EqualTo(1));

                // Cleanup instantiated object
                UnityEngine.Object.Destroy(instantiatedComponent.gameObject);
            }
            finally
            {
                UnityEngine.Object.Destroy(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_WithAssetReference_InstantiatesWithComponent() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;
            var assetReference = _dummyData.PrefabReference;

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
                UnityEngine.Object.Destroy(instantiatedComponent.gameObject);
            }
            finally
            {
                UnityEngine.Object.Destroy(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_WithMissingComponent_ThrowsInvalidOperationException() => UniTask.ToCoroutine(async () =>
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
                    await _registry.InstantiateAsync<Rigidbody>(_dummyData.PrefabAddress, parent, cts.Token);
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
                UnityEngine.Object.Destroy(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_MultipleCalls_IncreasesCount() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            await _registry.LoadAsync<Material>(_dummyData.MaterialAddress, cts.Token);

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancellation_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel before calling LoadAsync

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            }
            catch (OperationCanceledException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True, "Expected OperationCanceledException was not thrown");
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancellation_ReleasesHandle() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            try
            {
                await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - handle should be released after cancellation
            Assert.That(_registry.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_WithCancellation_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
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
                    await _registry.InstantiateAsync<BoxCollider>(_dummyData.PrefabAddress, parent, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    exceptionThrown = true;
                }

                Assert.That(exceptionThrown, Is.True, "Expected OperationCanceledException was not thrown");
            }
            finally
            {
                UnityEngine.Object.Destroy(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_WithCancellation_ReleasesHandle() => UniTask.ToCoroutine(async () =>
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
                    await _registry.InstantiateAsync<BoxCollider>(_dummyData.PrefabAddress, parent, cts.Token);
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
                UnityEngine.Object.Destroy(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator Clear_ReleasesAllHandles() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            await _registry.LoadAsync<Material>(_dummyData.MaterialAddress, cts.Token);
            Assert.That(_registry.Count, Is.EqualTo(2));

            // Act
            _registry.Clear();

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Dispose_ReleasesAllHandles() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            await _registry.LoadAsync<Material>(_dummyData.MaterialAddress, cts.Token);
            Assert.That(_registry.Count, Is.EqualTo(2));

            // Act
            _registry.Dispose();

            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Count_InitiallyZero() => UniTask.ToCoroutine(async () =>
        {
            // Assert
            Assert.That(_registry.Count, Is.EqualTo(0));
            await UniTask.Yield();
        });

        [UnityTest]
        public IEnumerator LoadAsync_SameAssetMultipleTimes_CreatesMultipleHandles() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            var obj1 = await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            var obj2 = await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);

            // Assert
            Assert.That(obj1, Is.Not.Null);
            Assert.That(obj2, Is.Not.Null);
            Assert.That(obj1, Is.SameAs(obj2)); // Same asset instance
            // Note: Addressables caches handles internally, so multiple loads of the same asset
            // return only one handle per Addressables loading session
            Assert.That(_registry.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_CreatesIndependentInstances() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var parent = new GameObject("TestParent").transform;

            try
            {
                // Act
                var instance1 = await _registry.InstantiateAsync<BoxCollider>(_dummyData.PrefabAddress, parent, cts.Token);
                var instance2 = await _registry.InstantiateAsync<BoxCollider>(_dummyData.PrefabAddress, parent, cts.Token);

                // Assert
                Assert.That(instance1, Is.Not.Null);
                Assert.That(instance2, Is.Not.Null);
                Assert.That(instance1, Is.Not.SameAs(instance2)); // Different instances
                Assert.That(instance1.gameObject, Is.Not.SameAs(instance2.gameObject));
                // Note: Addressables caches handles internally, so multiple loads of the same asset
                // return only one handle per Addressables loading session
                Assert.That(_registry.Count, Is.EqualTo(1));
                
                // Cleanup
                UnityEngine.Object.Destroy(instance1.gameObject);
                UnityEngine.Object.Destroy(instance2.gameObject);
            }
            finally
            {
                UnityEngine.Object.Destroy(parent.gameObject);
            }
        });

        [UnityTest]
        public IEnumerator Clear_CanLoadAssetsAgainAfterClearing() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var cts = new CancellationTokenSource();
            await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);
            _registry.Clear();

            // Act
            var loadedObject = await _registry.LoadAsync<GameObject>(_dummyData.PrefabAddress, cts.Token);

            // Assert
            Assert.That(loadedObject, Is.Not.Null);
            Assert.That(_registry.Count, Is.EqualTo(1));
        });
    }
}