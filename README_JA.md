# AssetsRegistry
[![unity-test](https://github.com/AndanteTribe/AssetsRegistry/actions/workflows/unity-test.yml/badge.svg)](https://github.com/AndanteTribe/AssetsRegistry/actions/workflows/unity-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/AssetsRegistry.svg)](https://github.com/AndanteTribe/AssetsRegistry/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/AssetsRegistry.svg)](./LICENSE)

[English](README.md) | 日本語

## 概要
**AssetsRegistry** は、Unity Addressables のアセットハンドルを使用中に一時的にキャッシュするためのユーティリティライブラリです。

Unity の Addressables システムを利用する際、`AsyncOperationHandle` を適切に管理しなければ、解放忘れによるメモリリークや、早期解放による不具合が発生します。**AssetsRegistry** は、特定のスコープ内でロードされたハンドルをすべて収集し、スコープ終了時（`Dispose` 呼び出し時）にまとめて解放することで、この問題を解消します。

## 要件
- Unity 2022.3 以上
- [Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) 1.19.19 以上
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.10 以上

## インストール
`Window > Package Manager` からPackage Managerウィンドウを開き、`[+] > Add package from git URL` を選択して以下のURLを入力します。

```
https://github.com/AndanteTribe/AssetsRegistry.git?path=src/AssetsRegistry.Unity/Packages/jp.andantetribe.assetsregistry
```

## クイックスタート

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
        // 1. プレハブをロードして手動でインスタンス化する例
        var prefab = await _registry.LoadAsync<GameObject>("assets/prefabs/MyPrefab.prefab", destroyCancellationToken);
        Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // 2. プレハブをロードし、特定のコンポーネントをインスタンス化する例
        //    (プレハブに MyComponent がアタッチされている前提)
        var component = await _registry.InstantiateAsync<MyComponent>("assets/prefabs/MyPrefabWithComponent.prefab", transform, destroyCancellationToken);
        component.transform.localPosition = Vector3.up;
    }

    private void OnDestroy()
    {
        // キャッシュされたすべてのハンドルをまとめて解放する
        _registry.Dispose();
    }
}
```

## API

| メソッド | 説明 |
|--------|------|
| `LoadAsync<TObject>(string address, CancellationToken cancellationToken)` | Addressables のアドレス文字列を指定してアセットを非同期でロードし、ハンドルをキャッシュします。 |
| `LoadAsync<TObject>(AssetReferenceT<TObject> reference, CancellationToken cancellationToken)` | `AssetReferenceT` を指定してアセットを非同期でロードし、ハンドルをキャッシュします。 |
| `InstantiateAsync<TComponent>(string address, Transform parent, CancellationToken cancellationToken)` | アドレス文字列からプレハブをロードし、インスタンス化して指定のコンポーネントを返します。ハンドルはキャッシュされます。 |
| `InstantiateAsync<TComponent>(AssetReferenceT<GameObject> reference, Transform parent, CancellationToken cancellationToken)` | `AssetReferenceT` からプレハブをロードし、インスタンス化して指定のコンポーネントを返します。ハンドルはキャッシュされます。 |
| `Clear()` | キャッシュされたすべてのアセットハンドルを解放し、レジストリをクリアします。 |
| `Dispose()` | `Clear()` と同等です。キャッシュされたすべてのアセットハンドルを解放します。 |

## ライセンス
このライブラリは、MITライセンスで公開しています。
