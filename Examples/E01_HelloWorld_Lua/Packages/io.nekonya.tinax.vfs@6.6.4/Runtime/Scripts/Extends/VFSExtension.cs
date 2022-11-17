#pragma warning disable SA1618
using System;
using TinaX.VFSKit.Exceptions;
using Cysharp.Threading.Tasks;

namespace TinaX.VFSKit
{
    /// <summary>
    /// An extension function for <see cref="VFS"/>.
    /// </summary>
    public static class VFSExtension
    {
        #region Load IAsset Async Callback

        public static void LoadAssetAsync<T>(this IVFS vfs, string assetPath, Action<IAsset> callback)
            where T : UnityEngine.Object
            => vfs.LoadAssetAsync(assetPath, typeof(T), callback);

        public static void LoadAssetAsync<T>(this IVFS vfs, string assetPath, Action<IAsset, VFSException> callback)
            where T : UnityEngine.Object
            => vfs.LoadAssetAsync(assetPath, typeof(T), callback);

        public static void LoadAssetAsync(this IVFS vfs, string assetPath, Type type, Action<IAsset> callback)
        {
            vfs.LoadAssetAsync(assetPath, type, (asset, _) =>
                {
                    callback(asset);
                }
            );
        }

        #endregion

        public static void LoadAsync<T>(this IVFS vfs,string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            vfs.LoadAsync<T>(assetPath, (asset, _) =>
                {
                    callback(asset);
                }
            );
        }

        public static void LoadAsync<T>(this IVFS vfs,string assetPath, Action<T, VFSException> callback) where T : UnityEngine.Object
        {
            vfs.LoadAsync(assetPath, typeof(T),(asset, exp) =>
                {
                    callback(asset as T,exp);
                }
            );
        }

        public static void LoadAsync(this IVFS vfs, string assetPath, Type type, Action<UnityEngine.Object> callback)
        {
            vfs.LoadAsync(assetPath, type, (asset, _) =>
                {
                    callback(asset);
                }
            );
        }

        public static void LoadAsync(this IVFS vfs, string assetPath, Type type,
            Action<UnityEngine.Object, VFSException> callback)
        {
            vfs.LoadAssetAsync(assetPath, type, (asset, exp) =>
                {
                    callback(asset.Get(),exp);
                }
            );
        }


        #region Load IAsset Async Task

        public static UniTask<IAsset> LoadAssetAsync<T>(this IVFS vfs, string assetPath) where T : UnityEngine.Object
            => vfs.LoadAssetAsync(assetPath, typeof(T));

        public static UniTask<IAsset> LoadAssetAsync(this IVFS vfs, string assetPath, Type type)
        {
            UniTaskCompletionSource<IAsset> tcs = new UniTaskCompletionSource<IAsset>();
            vfs.LoadAssetAsync(assetPath,type,(asset,_) =>
            {
                tcs.TrySetResult(asset);
            });
            return tcs.Task;
        }
        #endregion

        #region Load Asset Async Task
        public static UniTask<T> LoadAsync<T>(this IVFS vfs,string assetPath) where T : UnityEngine.Object
        {
            UniTaskCompletionSource<T> tcs = new UniTaskCompletionSource<T>();
            vfs.LoadAssetAsync<T>(assetPath,(asset) =>
            {
                tcs.TrySetResult(asset.Get<T>());
            });
            return tcs.Task;
        }

        public static UniTask<UnityEngine.Object> LoadAsync(this IVFS vfs, string assetPath, Type type)
        {
            UniTaskCompletionSource<UnityEngine.Object> tcs = new UniTaskCompletionSource<UnityEngine.Object>();
            vfs.LoadAssetAsync(assetPath,type,(asset) =>
            {
                tcs.TrySetResult(asset.Get());
            });
            return tcs.Task;

        }
        #endregion

        public static UniTask<ISceneAsset> LoadSceneAsync(this IVFS vfs, string scenePath)
        {
            UniTaskCompletionSource<ISceneAsset> tcs = new UniTaskCompletionSource<ISceneAsset>();
            vfs.LoadSceneAsync(scenePath,(bytes,_) =>
            {
                tcs.TrySetResult(bytes);
            });
            return tcs.Task;
        }
    }
}
