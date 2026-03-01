# AssetsRegistry
[![unity-test](https://github.com/AndanteTribe/AssetsRegistry/actions/workflows/unity-test.yml/badge.svg)](https://github.com/AndanteTribe/AssetsRegistry/actions/workflows/unity-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/AssetsRegistry.svg)](https://github.com/AndanteTribe/AssetsRegistry/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/AssetsRegistry.svg)](./LICENSE)
[![openupm](https://img.shields.io/npm/v/jp.andantetribe.assetsregistry?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/jp.andantetribe.assetsregistry/)

English | [日本語](README_JA.md)

## Overview
**AssetsRegistry** is a Unity Addressables utility for temporarily caching asset handles during usage.

When using Unity's Addressables system, you need to manage `AsyncOperationHandle` instances carefully — failing to release them causes memory leaks, while releasing them too early causes issues. **AssetsRegistry** solves this by collecting all handles loaded within a given scope and releasing them all at once when the scope ends (via `Dispose`).

## Requirements
- Unity 2022.3 or later
- [Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) 1.19.19 or later
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.10 or later

## Installation
Open `Window > Package Manager`, select `[+] > Add package from git URL`, and enter the following URL:

```
https://github.com/AndanteTribe/AssetsRegistry.git?path=src/AssetsRegistry.Unity/Packages/jp.andantetribe.assetsregistry
```

## Quick Start

```csharp
using System.Threading;
using AndanteTribe.Unity.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AssetsRegistrySample : MonoBehaviour
{
    private readonly AssetsRegistry _registry = new AssetsRegistry();

    private async UniTaskVoid Start()
    {
        // 1. Load a prefab and instantiate it manually
        var prefab = await _registry.LoadAsync<GameObject>("assets/prefabs/MyPrefab.prefab", destroyCancellationToken);
        Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // 2. Load a prefab and instantiate a specific component from it
        //    (assumes that MyComponent is attached to the prefab)
        var component = await _registry.InstantiateAsync<MyComponent>("assets/prefabs/MyPrefabWithComponent.prefab", transform, destroyCancellationToken);
        component.transform.localPosition = Vector3.up;
    }

    private void OnDestroy()
    {
        // Releases all cached handles at once
        _registry.Dispose();
    }
}
```

## API

| Method | Description |
|--------|-------------|
| `LoadAsync<TObject>(string address, CancellationToken cancellationToken)` | Loads an asset asynchronously by its Addressables address string and caches the handle. |
| `LoadAsync<TObject>(AssetReferenceT<TObject> reference, CancellationToken cancellationToken)` | Loads an asset asynchronously by its `AssetReferenceT` and caches the handle. |
| `InstantiateAsync<TComponent>(string address, Transform parent, CancellationToken cancellationToken)` | Loads a prefab by address string, instantiates it, and returns the specified component. The handle is cached. |
| `InstantiateAsync<TComponent>(AssetReferenceT<GameObject> reference, Transform parent, CancellationToken cancellationToken)` | Loads a prefab by `AssetReferenceT`, instantiates it, and returns the specified component. The handle is cached. |
| `Clear()` | Releases all cached asset handles and clears the registry. |
| `Dispose()` | Equivalent to `Clear()`. Releases all cached asset handles. |

## License
This library is released under the MIT license.
