using System;
using TinaX.VFSKit.Exceptions;

namespace TinaX.VFSKit
{
    public interface IVFS
    {
        #region About GC
        void Release(UnityEngine.Object asset);
        void UnloadUnusedAssets();
        #endregion

        #region Groups
        IGroup[] GetAllGroups();
        bool TryGetGroup(string groupName, out IGroup group);
        #endregion

        #region Load Asset Async Callback
        void LoadAssetAsync(string assetPath, Type type, Action<IAsset, VFSException> callback);
        #endregion

        #region Load Scene
        void LoadSceneAsync(string scenePath, Action<ISceneAsset, XException> callback);
        #endregion
    }
}

