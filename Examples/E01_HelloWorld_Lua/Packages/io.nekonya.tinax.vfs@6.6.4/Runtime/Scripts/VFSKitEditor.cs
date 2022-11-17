using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TinaX.IO;
using TinaX.VFSKit.Const;
using TinaX.VFSKit.Exceptions;
using TinaX.VFSKitInternal;
using System;
using UnityEngine;
using TinaX.VFSKitInternal.Utils;
using UniRx;
using Cysharp.Threading.Tasks;
using FileNotFoundException = TinaX.VFSKit.Exceptions.FileNotFoundException;

namespace TinaX.VFSKit
{
    public class VFSKitEditor : IVFS, IVFSInternal
    {
        private VFSConfigJson mConfig;

        /// <summary>
        /// 所有组的对象
        /// </summary>
        private List<VFSGroup> mGroups = new List<VFSGroup>();
        private Dictionary<string, VFSGroup> mDict_Groups = new Dictionary<string, VFSGroup>();

        /// <summary>
        /// 获取 assetbundleManifest的下载url （不包含开头的https://xxx/
        /// </summary>
        internal BundlesManager Bundles { get; private set; } = new BundlesManager();
        internal AssetsManager Assets { get; private set; } = new AssetsManager();

        private bool mInited = false;


        public VFSKitEditor()
        {
        }

        #region 生命周期

        /// <summary>
        /// 启动，如果初始化失败，则返回false.
        /// </summary>
        /// <returns></returns>
        public async Task<XException> Start()
        {
            if (mInited) return null;

            #region Version
            #endregion

            #region Configs
            // load config by xconfig | VFS not ready, so vfs config can not load by vfs.
            VFSConfigJson myConfig = null;
            var config = XConfig.GetConfig<VFSConfigModel>(VFSConst.ConfigFilePath_Resources);
            if (config == null)
            {
                return new VFSException("Load VFS config failed");
            }
            string json = JsonUtility.ToJson(config);
            myConfig = JsonUtility.FromJson<VFSConfigJson>(json);
            if(myConfig.Groups != null)
            {
                List<VFSGroupOption> options = new List<VFSGroupOption>();
                foreach(var option in myConfig.Groups)
                {
                    if (!option.ExtensionGroup)
                        options.Add(option);
                }
                myConfig.Groups = options.ToArray();
            }

            try
            {
                await UseConfig(myConfig);
            }
            catch (VFSException e)
            {
                return e;
            }


            #endregion

            mInited = true;
            return null;
        }


        #endregion


        #region 各种各样的对外的资源加载方法
        //异步加载======================================================================================================
        /// <summary>
        /// 加载【IAsset】,异步Task， 非泛型，【所有 非泛型 异步方法都是从这里封装出去的】
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>

        public void LoadAssetAsync(string assetPath, Type type, Action<IAsset,VFSException > callback)
        {
            this.loadAssetFromAssetDatabaseAsync(assetPath,type)
                .ToObservable()
                //.SubscribeOnMainThread()
                .ObserveOnMainThread()
                .Subscribe(asset =>
                {
                    callback?.Invoke(asset, null);
                }, e =>
                {
                    if (e is VFSException)
                    {
                        callback?.Invoke(null, (VFSException)e);
                    }
                    else
                        throw e;
                });
        }

        #endregion

        #region 特殊资源和封装规则

        public void LoadSceneAsync(string scenePath, Action<ISceneAsset, XException> callback)
        {
            this.loadSceneFromEditorAsync(scenePath)
                .ToObservable()
                .ObserveOnMainThread()
                .Subscribe(scene =>
                {
                    callback?.Invoke(scene, null);
                },
                e =>
                {
                    if (e is XException)
                        callback?.Invoke(null, e as XException);
                    else
                        Debug.LogException(e);
                });
        }

        #endregion

        #region GC相关
        public void Release(UnityEngine.Object asset)
        {
            if (asset == null)
                return;
            if (this.Assets.TryGetEditorAsset(asset.GetHashCode(), out var editor_asset))
                editor_asset.Release();

        }

        public void UnloadUnusedAssets()
        {
            this.Assets.Refresh();
            this.Bundles.Refresh();
        }

        #endregion


        #region 组 相关
        public IGroup[] GetAllGroups()
        {
            List<IGroup> groups = new List<IGroup>();
            foreach (var group in mGroups)
                groups.Add(group);
            return groups.ToArray();
        }

        public bool TryGetGroup(string groupName, out IGroup group)
        {
            if (this.mDict_Groups.TryGetValue(groupName, out var _group))
            {
                group = _group;
                return true;
            }

            group = null;
            return false;
        }
        #endregion


        #region 调试相关
        public List<VFSBundle> GetAllBundle()
        {
            return this.Bundles.GetVFSBundles();
        }

        public bool LoadFromAssetbundle()
        {
            return false;
        }

        #endregion

        public List<EditorAsset> GetAllEditorAsset()
        {
            return this.Assets.GetAllEditorAssets();
        }

        /// <summary>
        /// 使用配置，如果有效，返回true
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task UseConfig(VFSConfigJson config)
        {
            VFSUtil.NormalizationConfig(config);

            if (!VFSUtil.CheckConfiguration(config, out var errorCode, out var folderError))
            {
                throw new VFSException("VFS Config Error:", errorCode);
            }
            mConfig = config;

            // init configs data.
            if (mGroups == null) mGroups = new List<VFSGroup>();
            if (mDict_Groups == null) mDict_Groups = new Dictionary<string, VFSGroup>();
            
            if (mConfig.Groups != null)
            {
                foreach (var groupOption in mConfig.Groups)
                {
                    if (!groupOption.ExtensionGroup)
                    {
                        var groups = mGroups.Where(g => g.GroupName == groupOption.GroupName);
                        if (groups.Count() > 0)
                        {
                            var group = groups.First();
                            group.SetOptions(groupOption);
                            if (!mDict_Groups.ContainsKey(group.GroupName))
                            {
                                mDict_Groups.Add(group.GroupName, group);
                            }
                        }
                        else
                        {
                            var group = new VFSGroup(groupOption);
                            mGroups.Add(group);
                            if (!mDict_Groups.ContainsKey(group.GroupName))
                            {
                                mDict_Groups.Add(group.GroupName, group);
                            }
                        }
                        
                    }
                }
            }

        }

        /// <summary>
        /// 查询资源
        /// </summary>
        /// <param name="path"></param>
        /// <param name="result"></param>
        /// <returns>is valid</returns>
        private bool QueryAsset(string path, out AssetQueryResult result, out VFSGroup matched_group)
        {
            result = new AssetQueryResult();
            result.AssetPath = path;
            result.AssetPathLower = path.ToLower();
            result.AssetExtensionName = XPath.GetExtension(result.AssetPathLower,true);

            //检查有没有被全局的规则拦下来
            if (this.IgnoreByGlobalConfig(result.AssetPathLower, result.AssetExtensionName))
            {
                result.Vliad = false;
                matched_group = null;
                return false;
            }

            VFSGroup myGroup = null;
            //有效组查询
            foreach(var group in mGroups)
            {
                if (group.IsAssetPathMatch(path))
                {
                    myGroup = group;
                    break;
                }
            }

            if(myGroup != null)
            {
                result.Vliad = true;
                result.GroupName = myGroup.GroupName;
                string assetbundle_name = myGroup.GetAssetBundleNameOfAsset(path, out var buildType, out var devType); //获取到的assetbundle是不带后缀的
                result.AssetBundleName = assetbundle_name + mConfig.AssetBundleFileExtension;
                result.AssetBundleNameWithoutExtension = assetbundle_name;
                result.DevelopType = devType;
                result.BuildType = buildType;
                result.ExtensionGroup = myGroup.ExtensionGroup;
                result.GroupHandleMode = myGroup.HandleMode;
                matched_group = myGroup;
                return true;
            }
            result.Vliad = false;
            matched_group = null;
            return false;
        }

        /// <summary>
        /// 是否被全局配置项所忽略
        /// </summary>
        /// <param name="path_lower">小写处理后的路径</param>
        /// <param name="extension">扩展名,传进来的是小写，以点开头的</param>
        /// <returns></returns>
        private bool IgnoreByGlobalConfig(string path_lower, string extension)
        {
            //后缀名忽略 //据说迭代器循环的效率比LINQ高，所以先用迭代器，有机会细测一下
            foreach(var item in mConfig.GlobalVFS_Ignore_ExtName) //在初始化过程中，配置中的数据都被规范化成小写，以点开头的格式，并且去掉了重复
            {
                if (item.Equals(extension)) return true;
            }

            //Path Item 
            if(mConfig.GlobalVFS_Ignore_Path_Item != null && mConfig.GlobalVFS_Ignore_Path_Item.Length > 0)
            {
                string[] path_items = path_lower.Split('/');
                foreach(var ignore_item in mConfig.GetGlobalVFS_Ignore_Path_Item(true, false)) //获取到的数据已经是小写，并且有缓存
                {
                    foreach(var path_item in path_items)
                    {
                        if (ignore_item.Equals(path_item)) return true;
                    }
                }
            }

            return false;
        }

#region 编辑器下的AssetDatabase加载
        private async Task<IAsset> loadAssetFromAssetDatabaseAsync(string asset_path, Type type)
        {
            var asset = loadAssetFromAssetDatabase(asset_path, type); //编辑器没提供异步接口，所以同步加载
            await UniTask.DelayFrame(1); //为了模拟异步，我们等待一帧（总之不能让调用它的地方变成同步方法）
            return asset;
        }

        private IAsset loadAssetFromAssetDatabase(string assetPath, Type type)
        {
            //要查询的
            if (this.QueryAsset(assetPath, out var result, out var group))
            {
                //查重
                if (this.Assets.TryGetEditorAsset(result.AssetPathLower, out var _editor_asset))
                {
                    _editor_asset.Retain();
                    return _editor_asset;
                }
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), assetPath)))
                    throw new FileNotFoundException("[TinaX.VFS]Connot load assets in editor:" + assetPath, assetPath);
                //加载
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, type);
                //登记
                var editor_asset = new EditorAsset(asset, result.AssetPathLower);
                editor_asset.Retain();
                this.Assets.Register(editor_asset);
                return editor_asset;
            }

            throw new VFSException(("The asset path you want to load is valid. It is not under the management of VFS")
                                   + "Path:"
                                   + assetPath, VFSErrorCode.ValidLoadPath);
        }

#endregion

#region 编辑器下Scene加载

        private async Task<ISceneAsset> loadSceneFromEditorAsync(string scenePath)
        {
            var asset = this.loadSceneFromEditor(scenePath);
            await UniTask.DelayFrame(1);
            return asset;
        }

        private ISceneAsset loadSceneFromEditor(string scenePath)
        {
            //要查询的
            if (this.QueryAsset(scenePath, out var result, out var group))
            {
                //查重
                if (this.Assets.TryGetEditorAsset(result.AssetPathLower, out var _editor_asset))
                {
                    _editor_asset.Retain();
                    return (EditorSceneAsset)_editor_asset;
                }
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                    throw new FileNotFoundException("[TinaX.VFS]Connot load assets in editor:" + scenePath, scenePath);
                //加载
                //什么都不用加载
                //登记
                var editor_asset = new EditorSceneAsset(result.AssetPath, result.AssetPathLower);
                editor_asset.Retain();
                this.Assets.Register(editor_asset);
                return editor_asset;
            }

            throw new VFSException(("The Scene path you want to load is valid. It is not under the management of VFS")
                                   + "Path:"
                                   + scenePath, VFSErrorCode.ValidLoadPath);
        }

#endregion
    }
}