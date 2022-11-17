/*
 * 你看啊，所有加载方法在最终实现的时候都按照泛型和非泛型写了两套，
 * 那为什么不直接把泛型的T用typeof(T)传给带Type参数的非泛型方法呢，这样就可以少写一遍了
 * 因为作者有强迫症，这么搞会觉得难受。
 */
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TinaX;
using TinaX.Utils;
using TinaX.IO;
using TinaX.Services;
using TinaX.VFSKit.Const;
using TinaX.VFSKit.Exceptions;
using TinaX.VFSKitInternal;
using System;
using UnityEngine;
using UnityEngine.Networking;
using TinaX.VFSKitInternal.Utils;
using UniRx;
using Cysharp.Threading.Tasks;
using FileNotFoundException = TinaX.VFSKit.Exceptions.FileNotFoundException;

namespace TinaX.VFSKit
{
    public class VFSKit : IVFS, IVFSInternal
    {
        public const string DefaultDownloadWebAssetUrl = "http://localhost:8080/";

        public XRuntimePlatform Platform { get; private set; }
        public string PlatformText { get; private set; }
        public string DownloadWebAssetUrl => webVfs_asset_download_base_url;
        public string VirtualDiskPath { get; private set; }

        public VFSCustomizable Customizable { get; private set; }

        public int DownloadWebAssetTimeout { get; set; } = 10;
        public int DownloadAssetBundleManifestTimeout { get; set; } = 5;
        public int DownloadAssetBundleFilesHashTimeout { get; set; } = 5;

        public bool Override_StreamingAssetsPath { get; private set; } = false;

        private string mVirtualDisk_MainPackageFolderPath;
        private string mVirtualDisk_DataFolderPath;
        private string mVirtualDisk_ExtensionGroupRootFolderPath;

        private bool? _ischinese;
        private bool IsChinese
        {
            get
            {
                if(_ischinese == null)
                {
                    _ischinese = (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified);
                }
                return _ischinese.Value;
            }
        }

        /// <summary>
        /// StreamingAssets -> packages root
        /// </summary>
        private string mStreamingAssets_PackagesRootFolderPath; //因为这个地址可能会被重写，所以必须要从这里读取。
        /// <summary>
        /// StreamingAssets -> vfs_root
        /// </summary>
        private string mStreamingAssets_MainPackageFolderPath;   //因为这个地址可能会被重写，所以必须要从这里读取。
        /// <summary>
        /// StreamingAssets -> vfs_extension
        /// </summary>
        private string mStreamingAssets_ExtensionGroupRootFolderPath;   //因为这个地址可能会被重写，所以必须要从这里读取。
        /// <summary>
        /// StreamingAssets -> vfs_data
        /// </summary>
        private string mStreamingAssets_DataRootFolderPath;   //因为这个地址可能会被重写，所以必须要从这里读取。


#if UNITY_EDITOR
        /// <summary>
        /// Editor下，使用UnityEditor.AssetDatabase 加载资源
        /// </summary>
        private bool mLoadByAssetDatabaseInEditor = false;
#endif


        private VFSConfigJson mConfig;

        /// <summary>
        /// 所有组的对象
        /// </summary>
        private List<VFSGroup> mGroups = new List<VFSGroup>();
        private Dictionary<string, VFSGroup> mDict_Groups = new Dictionary<string, VFSGroup>();


        private string webVfs_asset_download_base_url;
        private bool webvfs_download_base_url_modify = false; //如果被profile或者手动修改过的话，这里为true

        /// <summary>
        /// 获取WebAssets的Url请调用这里的委托变量
        /// </summary>
        internal GetWebAssetDownloadUrlDelegate GetWebAssetUrl;
        /// <summary>
        /// 获取hash file 的url请调用这个（不包含开头的https://xxx/
        /// </summary>
        internal GetFileHashDownloadUrlDalegate GetWebFileHashBookUrl;
        /// <summary>
        /// 获取 assetbundleManifest的下载url （不包含开头的https://xxx/
        /// </summary>
        internal GetAssetBundleManifestDownloadUrlDalegate GetAssetBundleManifestDoanloadUrl;
        internal BundlesManager Bundles { get; private set; } = new BundlesManager();
        internal AssetsManager Assets { get; private set; } = new AssetsManager();
        internal ExtensionGroupsManager ExtensionGroups { get; private set; }
        private PackageVersionInfo MainPackage_Version; //这个信息只会存在于StreamingAssets
        private BuildInfo MainPackage_BuildInfo;//这个信息只会存在于StreamingAssets
        private VFSUpgrdableVersionInfo mUpgradeable_Version_Info; //VDisk中的资源是母包和补丁的结合信息。

        private string mVFS_Patch_Handle_Folder;
        private string mVFS_Patch_Temp_Folder;

        private bool mInited = false;
        private bool mWebVFSReady = false;


        public VFSKit()
        {
            ExtensionGroups = new ExtensionGroupsManager(this); 
            Customizable = new VFSCustomizable(this);
            this.GetWebAssetUrl = default_getWebAssetUrl;
            this.GetWebFileHashBookUrl = default_getWebFilesHashUrl;
            this.GetAssetBundleManifestDoanloadUrl = default_getAssetBundleManifestDoanloadUrl;

            mStreamingAssets_PackagesRootFolderPath = VFSUtil.GetPackagesRootFolderInStreamingAssets();
            mStreamingAssets_MainPackageFolderPath = VFSUtil.GetMainPackageFolderInStreamingAssets();
            mStreamingAssets_DataRootFolderPath = VFSUtil.GetDataFolderInStreamingAssets();
            mStreamingAssets_ExtensionGroupRootFolderPath = VFSUtil.GetExtensionPackageRootFolderInStreamingAssets();

            Platform = XPlatformUtil.GetXRuntimePlatform(Application.platform);
            PlatformText = XPlatformUtil.GetNameText(Platform);

            mVFS_Patch_Handle_Folder = Path.Combine(XCore.LocalStorage_TinaX, "VFS_Patch");
            mVFS_Patch_Temp_Folder = Path.Combine(mVFS_Patch_Handle_Folder, "temp");
#if UNITY_EDITOR
            //load mode
            var loadMode = VFSLoadModeInEditor.GetLoadMode();
            switch (loadMode)
            {
                case RuntimeAssetsLoadModeInEditor.LoadByAssetDatabase:
                    mLoadByAssetDatabaseInEditor = true;
                    Debug.Log("[TinaX] VFS:" + (IsChinese ? $"<color=#{Internal.XEditorColorDefine.Color_Emphasize_16}>基于编辑器策略，采用编辑器方式加载资源</color>" : $"<color=#{Internal.XEditorColorDefine.Color_Emphasize_16}>Load assets by UnityEditor.AssetDatabase</color>"));
                    break;
                case RuntimeAssetsLoadModeInEditor.Normal:
                    mLoadByAssetDatabaseInEditor = false;
                    break;
                case RuntimeAssetsLoadModeInEditor.Override_StreamingAssetsPath:
                    Override_StreamingAssetsPath = true;
                    mStreamingAssets_PackagesRootFolderPath = VFSLoadModeInEditor.Get_Override_StreamingAssets_PackasgeRootFolderPath();
                    mStreamingAssets_MainPackageFolderPath = VFSUtil.GetMainPackageFolderInPackages(mStreamingAssets_PackagesRootFolderPath);
                    mStreamingAssets_DataRootFolderPath = VFSUtil.GetDataFolderInPackages(mStreamingAssets_PackagesRootFolderPath);
                    mStreamingAssets_ExtensionGroupRootFolderPath = VFSUtil.GetExtensionPackageRootFolderInPackages(mStreamingAssets_PackagesRootFolderPath);
                    break;
            }
#endif
        }

        #region 生命周期

        /// <summary>
        /// 启动，如果初始化失败，则返回false.
        /// </summary>
        /// <returns></returns>
        public async Task<XException> Start()
        {
            if (mInited) return null;

            #region virtual disk
            //init vfs virtual disk folder
            VirtualDiskPath = Path.Combine(XCore.LocalStorage_TinaX, "VFS_VDisk"); //TODO: 在Windows之类目录权限比较自由的平台，未来可以考虑搞个把这个目录移动到别的地方的功能。（毕竟有人不喜欢把太多文件扔在C盘）
            XDirectory.CreateIfNotExists(VirtualDiskPath);
            mVirtualDisk_MainPackageFolderPath = VFSUtil.GetMainPackageFolderInPackages(VirtualDiskPath);
            mVirtualDisk_DataFolderPath = VFSUtil.GetDataFolderInPackages(VirtualDiskPath);
            mVirtualDisk_ExtensionGroupRootFolderPath = VFSUtil.GetExtensionPackageRootFolderInPackages(VirtualDiskPath);
            #endregion


            #region Version
            bool init_versions = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_versions = false;
#endif
            if (init_versions)
            {
                try
                {
                    string version_path = VFSUtil.GetMainPackage_VersionInfo_Path(mStreamingAssets_PackagesRootFolderPath);
                    string json = await LoadTextFromStreamingAssetsAsync(version_path);
                    this.MainPackage_Version = JsonUtility.FromJson<PackageVersionInfo>(json);

                    string build_info_path = VFSUtil.GetMainPackage_BuildInfo_Path(mStreamingAssets_PackagesRootFolderPath);
                    string json_binfo = await LoadTextFromStreamingAssetsAsync(build_info_path);
                    this.MainPackage_BuildInfo = JsonUtility.FromJson<BuildInfo>(json_binfo);
                    if(this.MainPackage_BuildInfo.BuildID != this.MainPackage_Version.buildId)
                    {
                        this.MainPackage_Version = null;
                        this.MainPackage_BuildInfo = default;
                        Debug.LogWarning(IsChinese ? "[TinaX.VFS]当前母包中的版本信息无效，它可能已过期，补丁功能将不可用" : "[TinaX.VFS]The version information of the main package package is invalid. It may have expired and the patch function will be unavailable.");
                    }
                }
                catch (FileNotFoundException)
                {
                    Debug.LogWarning(IsChinese ? "[TinaX.VFS]当前母包没有登记版本信息，补丁功能将不可用。" : "[TinaX.VFS]The current main package has no registered version information, and the patch function will not be available.");
                }
                catch (VFSException e)
                {
                    return e;
                }

                //VDisk
                if (this.MainPackage_Version != null)
                {
                    string vdisk_version_path = VFSUtil.GetMainPackage_UpgradableVersionFilePath(VirtualDiskPath);
                    if (File.Exists(vdisk_version_path))
                    {
                        this.mUpgradeable_Version_Info = XConfig.GetJson<VFSUpgrdableVersionInfo>(vdisk_version_path, AssetLoadType.SystemIO, false);
                        //检查一致性
                        if(this.mUpgradeable_Version_Info.VFSPackageVersion != this.MainPackage_Version.version)
                        {
                            this.mUpgradeable_Version_Info.VFSPackageVersion = this.MainPackage_Version.version;
                            this.mUpgradeable_Version_Info.VFSPackageVersionName = this.MainPackage_Version.versionName;
                            this.mUpgradeable_Version_Info.VFSPatchVersion = -1;
                            this.mUpgradeable_Version_Info.VFSPatchVersionName = string.Empty ;
                            XConfig.SaveJson(this.mUpgradeable_Version_Info, vdisk_version_path);

                            //因为母包换了，之前的资源不能用了，资源全清掉
                            XDirectory.DeleteIfExists(mVirtualDisk_DataFolderPath);
                            XDirectory.DeleteIfExists(mVirtualDisk_MainPackageFolderPath);
                        }
                    }
                    else
                    {
                        //创建这个文件
                        this.mUpgradeable_Version_Info = new VFSUpgrdableVersionInfo()
                        {
                            VFSPackageVersionName = this.MainPackage_Version.versionName,
                            VFSPackageVersion = this.MainPackage_Version.version,
                            VFSPatchVersionName = string.Empty,
                            VFSPatchVersion = -1
                        };
                        XConfig.SaveJson(this.mUpgradeable_Version_Info, vdisk_version_path);
                    }
                }
                
            }

            #endregion


            #region Configs
            // load config by xconfig | VFS not ready, so vfs config can not load by vfs.
            VFSConfigJson myConfig = null;
            bool load_config_runtime = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor)
            {
                load_config_runtime = false;
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
            }
#endif
            if (load_config_runtime)
            {
                string config_path_vdisk = VFSUtil.GetVFSConfigFilePath_InPackages(VirtualDiskPath);
                if (File.Exists(config_path_vdisk))
                {
                    myConfig = XConfig.GetJson<VFSConfigJson>(config_path_vdisk);
                }
                else
                {
                    try
                    {
                        var config_json = await LoadTextFromStreamingAssetsAsync(VFSUtil.GetVFSConfigFilePath_InPackages(mStreamingAssets_PackagesRootFolderPath));
                        myConfig = JsonUtility.FromJson<VFSConfigJson>(config_json);
                    }
                    catch (FileNotFoundException)
                    {
                        return new VFSException("Load VFS config failed");
                    }

                }
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

            #region Patch
            XDirectory.DeleteIfExists(mVFS_Patch_Temp_Folder, true);
            #endregion





            bool need_init_webvfs = false;
            if (mConfig.InitWebVFSOnStart)
            {
                need_init_webvfs = true;
#if UNITY_EDITOR
                if (mLoadByAssetDatabaseInEditor) need_init_webvfs = false;
#endif
            }
            if (need_init_webvfs)
            {
                try
                {
                    await InitWebVFS();
                }
                catch (VFSException e)
                {
#if UNITY_EDITOR
                    Debug.LogError(e.Message);
#endif
                    return e;
                }
            }

            mInited = true;
            return null;
        }


        #endregion

        public async Task InitWebVFS()
        {
            Debug.Log("[TinaX.VFS] Init Web VFS ...");
            #region WebVFS Url
            var web_vfs_config = XConfig.GetConfig<WebVFSNetworkConfig>(VFSConst.Config_WebVFS_URLs);
            if (web_vfs_config != null)
            {
                await this.UseWebVFSNetworkConfig(web_vfs_config);
            }
            #endregion

            #region WebVFS FileHash
            List<Task> tasks = new List<Task>();
            foreach(var group in mGroups)
            {
                tasks.Add(InitGroupManifestRemote(group));
                tasks.Add(InitGroupFilesHashRemote(group));
            }
            foreach(var group in this.ExtensionGroups.mGroups)
            {
                if (group.WebVFS_Available)
                {
                    tasks.Add(InitGroupManifestRemote(group));
                    tasks.Add(InitGroupFilesHashRemote(group));
                }
            }

            await Task.WhenAll(tasks);
            #endregion

            Debug.Log("Web VFS Inited");
            mWebVFSReady = true;
        }

        #region 各种各样的对外的资源加载方法
        //异步加载======================================================================================================
        /// <summary>
        /// 加载【IAsset】,异步Task， 非泛型，【所有 非泛型 异步方法都是从这里封装出去的】
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<IAsset> LoadAssetAsync(string assetPath, Type type) //
        {
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor)
            {
                return await this.loadAssetFromAssetDatabaseAsync(assetPath, type);
            }
#endif
            return await this.loadAssetAsync(assetPath, type);
        }

        public void LoadAssetAsync(string assetPath, Type type, Action<IAsset,VFSException > callback)
        {
            this.LoadAssetAsync(assetPath,type)
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
        public async Task<ISceneAsset> LoadSceneAsync(string scenePath)
        {
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor)
                return await this.loadSceneFromEditorAsync(scenePath);
#endif
            return await this.loadSceneAsync(scenePath);
        }

        public void LoadSceneAsync(string scenePath, Action<ISceneAsset, XException> callback)
        {
            this.LoadSceneAsync(scenePath)
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
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor)
            {
                if (asset == null)
                    return;
                if (this.Assets.TryGetEditorAsset(asset.GetHashCode(), out var editor_asset))
                    editor_asset.Release();
                return;
            }
#endif
            if (this.Assets.TryGetAsset(asset.GetHashCode(), out var vfs_asset))
                vfs_asset.Release();
            else if (this.Assets.TryGetAssetSync(asset.GetHashCode(), out var vfs_asset_sync))
                vfs_asset_sync.Release();
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
            foreach (var group in this.ExtensionGroups.mGroups)
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
            else
            {
                if (this.ExtensionGroups.TryGetExtensionGroup(groupName, out var ex_group))
                {
                    group = ex_group;
                    return true;
                }
                else
                {
                    group = null;
                    return false;
                }
            }
        }
        #endregion

        #region 直接加载

        /// <summary>
        /// 从StreamingAssets中直接加载文件，使用StreamingAssets的相对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<byte[]> LoadFileFromStreamingAssetsAsync(string path)
        {
            string load_path = this.getFilePathForStreamingAssets(path);
            byte[] data = await this.loadFileFromStreamingAssetsAsync(load_path);
            return data;
        }

        public void LoadFileFromStreamingAssetsAsync(string path, Action<byte[], VFSException> callback)
        {
            this.LoadFileFromStreamingAssetsAsync(path)
                .ToObservable()
                .ObserveOnMainThread()
                .Subscribe(data =>
                {
                    callback?.Invoke(data, null);
                }, e =>
                {
                    if (e is VFSException)
                        callback?.Invoke(null, e as VFSException);
                    else
                        Debug.LogException(e);
                });
        }

        #endregion

        #region 调试相关
        public List<VFSBundle> GetAllBundle()
        {
            return this.Bundles.GetVFSBundles();
        }

        public bool LoadFromAssetbundle()
        {
#if UNITY_EDITOR
            return !mLoadByAssetDatabaseInEditor;
#else
            return true;
#endif
        }

        #endregion

#if UNITY_EDITOR
        public List<EditorAsset> GetAllEditorAsset()
        {
            return this.Assets.GetAllEditorAssets();
        }
#endif


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

#region Manifest and FilesHash ...
            List<Task> list_init_manifest_and_hashs_tasks = new List<Task>();
            if (mGroups != null && mGroups.Count > 0)
            {
                foreach (var group in mGroups)
                {
                    var task_manifest = InitGroupManifests(group);
                    list_init_manifest_and_hashs_tasks.Add(task_manifest);
                    var task_hash = InitGroupFilesHash(group);
                    list_init_manifest_and_hashs_tasks.Add(task_hash);
                }
            }
            await Task.WhenAll(list_init_manifest_and_hashs_tasks);
#endregion


            if (!webvfs_download_base_url_modify)
            {
                webVfs_asset_download_base_url = mConfig.DefaultWebVFSBaseUrl;
            }
        }

        public void SetDownloadWebAssetUrl(string url)
        {
            webvfs_download_base_url_modify = true;
            webVfs_asset_download_base_url = url;
            if (!webVfs_asset_download_base_url.EndsWith("/"))
            {
                webVfs_asset_download_base_url += "/";
            }
        }

        public async Task UseWebVFSNetworkConfig(WebVFSNetworkConfig config)
        {
            bool flag = false;
            if (config != null || config.Configs != null && config.Configs.Length > 0)
            {
                //尝试获取profile
                string profile = XCore.GetMainInstance().ProfileName;
                bool developMode = XCore.GetMainInstance().DevelopMode;
                var results = config.Configs.Where(item => item.ProfileName == profile);
                if(results.Count() > 0)
                {
                    var result = results.First();
                    //挨个寻找合适的服务器
                    if(result.Urls!= null && result.Urls.Length > 0)
                    {
                        foreach(var item in result.Urls)
                        {
                            if (item.NetworkMode == WebVFSNetworkConfig.NetworkMode.DevelopMode && !developMode)
                                continue;
                            if (item.NetworkMode == WebVFSNetworkConfig.NetworkMode.Editor && !Application.isEditor)
                                continue;

                            bool b = await SayHelloToWebServer(item.HelloUrl, 4);
                            if (b)
                            {
                                flag = true;
                                webVfs_asset_download_base_url = item.BaseUrl;
                                break;
                            }
                        }
                    }
                }
            }


            if (!flag)
            {
                //执行到这里，没有在prefile配置中找到有效的url的话，就用mconfig里的
                webVfs_asset_download_base_url = mConfig.DefaultWebVFSBaseUrl;
            }

            if(webVfs_asset_download_base_url.IsNullOrEmpty() || webVfs_asset_download_base_url.IsNullOrWhiteSpace())
                webVfs_asset_download_base_url = DefaultDownloadWebAssetUrl;

            if (!webVfs_asset_download_base_url.EndsWith("/"))
                webVfs_asset_download_base_url += "/";

            webvfs_download_base_url_modify = true;
        }

        private async UniTask<byte[]> loadFileFromStreamingAssetsAsync(string path)
        {
            try
            {
                using(var req = UnityWebRequest.Get(new Uri(path)))
                {
                    await req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                    if (req.result == UnityWebRequest.Result.ProtocolError)
#else
                if (req.isHttpError)
#endif
                    {
                        if (req.responseCode == 404)
                            throw new Exceptions.FileNotFoundException($"Failed to load file from StreamingAssets, file path:{path}", path);
                    }
                    return req.downloadHandler.data;
                }
            }
            catch(UnityWebRequestException e)
            {
#if UNITY_2020_2_OR_NEWER
                if(e.Result == UnityWebRequest.Result.ProtocolError)
#else
                if (e.IsHttpError)
#endif
                {
                    if (e.UnityWebRequest.responseCode == 404)
                        throw new Exceptions.FileNotFoundException($"Failed to load file from StreamingAssets, file path:{path}", path);
                }
                throw e;
            }
        }

        private async UniTask<string> LoadTextFromStreamingAssetsAsync(string path)
        {
            try
            {
                using (var req = UnityWebRequest.Get(new Uri(path)))
                {
                    await req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                    if (req.result == UnityWebRequest.Result.ProtocolError)
#else
                if (req.isHttpError)
#endif
                    {
                        if (req.responseCode == 404)
                            throw new Exceptions.FileNotFoundException($"Failed to load file from StreamingAssets, file path:{path}", path);
                    }
                    return StringHelper.RemoveUTF8BOM(req.downloadHandler.data);
                }
                
            }
            catch(UnityWebRequestException e)
            {
#if UNITY_2020_2_OR_NEWER
                if(e.Result == UnityWebRequest.Result.ProtocolError)
#else
                if (e.IsHttpError)
#endif
                {
                    if (e.UnityWebRequest.responseCode == 404)
                        throw new Exceptions.FileNotFoundException($"Failed to load file from StreamingAssets, file path:{path}", path);
                }

                throw e;
            }
            
        }


        private  async UniTask<string> DownLoadTextFromWebAsync(Uri uri, int timeout = 3, Encoding encoding = null)
        {
            try
            {
                //Debug.Log("喵，下载文本：" + uri.ToString());
                using(var req = UnityWebRequest.Get(uri))
                {
                    req.timeout = timeout;
                    await req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                    {
                        if (req.responseCode == 404)
                            throw new FileNotFoundException("Failed to get text from web : " + uri.ToString(), uri.ToString());
                        else
                            throw new VFSException("Failed to get text from web:" + uri.ToString());
                    }
                    if (encoding == null)
                        return StringHelper.RemoveUTF8BOM(req.downloadHandler.data);
                    else
                    {
                        if (encoding == Encoding.UTF8)
                            return StringHelper.RemoveUTF8BOM(req.downloadHandler.data);
                        else
                            return encoding.GetString(req.downloadHandler.data);
                    }
                }
                
            }
            catch(UnityWebRequestException e)
            {
#if UNITY_2020_2_OR_NEWER
                if(e.Result != UnityWebRequest.Result.Success)
#else
                if (e.IsNetworkError || e.IsHttpError)
#endif
                {
                    if (e.UnityWebRequest.responseCode == 404)
                        throw new FileNotFoundException("Failed to get text from web : " + uri.ToString(), uri.ToString());
                    else
                        throw new VFSException("Failed to get text from web:" + uri.ToString());
                }
                throw e;
            }

        }

        private async Task InitGroupManifests(VFSGroup group)
        {


            bool init_vdisk = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_vdisk = false;
#endif
            //vdisk manifest
            if (init_vdisk)
            {
                string vdisk_manifest_path = group.GetManifestFilePath(VirtualDiskPath);
                if (File.Exists(vdisk_manifest_path))
                {
                    try
                    {
                        string json = File.ReadAllText(vdisk_manifest_path);
                        var bundleManifest = JsonUtility.FromJson<BundleManifest>(json);
                        group.Manifest_VirtualDisk = new XAssetBundleManifest(bundleManifest);
                    }
                    catch { /* do nothing */ }
                }
            }

            bool init_streamingassets = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_streamingassets = false;
#endif
            if (group.Manifest_VirtualDisk != null) init_streamingassets = true;
            //streamingassets assetbundleManifest
            if (init_streamingassets)
            {
                string stream_manifest_path = group.GetManifestFilePath(mStreamingAssets_PackagesRootFolderPath);
                try
                {
                    string stream_json = await LoadTextFromStreamingAssetsAsync(stream_manifest_path);
                    var bundleManifest = JsonUtility.FromJson<BundleManifest>(stream_json);
                    group.Manifest_StreamingAssets = new XAssetBundleManifest(bundleManifest);
                }
                catch (FileNotFoundException) { /* do nothing */ }
            }

            //remote?
            bool init_remote = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_remote = false;
#endif
            if (group.HandleMode == GroupHandleMode.LocalAndUpdatable || group.HandleMode == GroupHandleMode.LocalOnly) init_remote = false;
            if (mWebVFSReady && init_remote)
            {

                await InitGroupManifestRemote(group);
            }
        }
        private async Task InitGroupFilesHash(VFSGroup group)
        {
            //vdisk manifest
            bool init_vdisk = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_vdisk = false;
#endif
            if (init_vdisk)
            {
                string vdisk_hash_path = group.GetAssetBundleHashsFilePath(VirtualDiskPath);
                if (File.Exists(vdisk_hash_path))
                {
                    try
                    {
                        string json = File.ReadAllText(vdisk_hash_path);
                        group.FilesHash_VirtualDisk = JsonUtility.FromJson<FilesHashBook>(json);
                    }
                    catch { /* do nothing */ }
                }
            }

            //streamingassets assetbundleManifest
            bool init_streamingassets = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_streamingassets = false;
#endif
            if (group.FilesHash_VirtualDisk != null) 
                init_streamingassets = false;
            if (init_streamingassets)
            {
                string hash_stream_path = group.GetAssetBundleHashsFilePath(mStreamingAssets_PackagesRootFolderPath);
                try
                {
                    string json = await LoadTextFromStreamingAssetsAsync(hash_stream_path);
                    group.FilesHash_StreamingAssets = JsonUtility.FromJson<FilesHashBook>(json);
                }
                catch (FileNotFoundException) { /* do nothing */ }
            }


            //remote?
            bool init_remote = true;
#if UNITY_EDITOR
            if (mLoadByAssetDatabaseInEditor) init_remote = false;
#endif
            if (group.HandleMode == GroupHandleMode.LocalAndUpdatable || group.HandleMode == GroupHandleMode.LocalOnly) init_remote = false;
            if (mWebVFSReady && init_remote)
            {
                await InitGroupFilesHashRemote(group);
            }
        }

        /// <summary>
        /// Editor Assetdatabase加载模式下别调用这里
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private async Task InitExtensionGroupManifest(VFSExtensionGroup group)
        {
            if (!group.OverridePackagePath)
            {
                //没有重写路径，就当成普通的group处理就完事了
                await this.InitGroupManifests(group);
                return;
            }

            //处理重写路径的，程序里把它算作virtual disk的数据
            string manifest_path = group.GetManifestFilePath(VirtualDiskPath);
            if (File.Exists(manifest_path))
            {
                group.Manifest_VirtualDisk = new XAssetBundleManifest(XConfig.GetJson<BundleManifest>(manifest_path, AssetLoadType.SystemIO, false));
            }
            else
            {
                throw new VFSException($"[TinaX.VFS] Cannot found AssetBundleManifest file from extension group: {group.GroupName} , folder path: {group.PackagePathSpecified}");
            }

            bool init_remote = false;
            if (group.HandleMode == GroupHandleMode.LocalAndUpdatable || group.HandleMode == GroupHandleMode.LocalOnly) init_remote = false;
            if (!group.WebVFS_Available) init_remote = false;
            if(init_remote && mWebVFSReady)
            {
                await InitGroupManifestRemote(group);
            }
        }

        private async Task InitExtensionGroupFilesHash(VFSExtensionGroup group)
        {
            if (!group.OverridePackagePath)
            {
                //没有重写路径，就当成普通的group处理就完事了
                await this.InitGroupFilesHash(group);
                return;
            }

            //处理重写路径的，程序里把它算作virtual disk的数据
            string manifest_path = group.GetManifestFilePath(VirtualDiskPath);
            if (File.Exists(manifest_path))
            {
                group.FilesHash_VirtualDisk = XConfig.GetJson<FilesHashBook>(manifest_path);
            }
            else
            {
                throw new VFSException($"[TinaX.VFS] Cannot found AssetBundleManifest file from extension group: {group.GroupName} , folder path: {group.PackagePathSpecified}");
            }

            bool init_remote = false;
            if (group.HandleMode == GroupHandleMode.LocalAndUpdatable || group.HandleMode == GroupHandleMode.LocalOnly) init_remote = false;
            if (!group.WebVFS_Available) init_remote = false;
            if (init_remote && mWebVFSReady)
            {
                await InitGroupFilesHashRemote(group);
            }
        }

        private async Task InitGroupManifestRemote(VFSGroup group)
        {
            string uri = this.GetWebAssetBundleManifestDoanloadUrl(this.PlatformText, group.ExtensionGroup, group.GroupName);
            string json = await DownLoadTextFromWebAsync(new Uri(uri), this.DownloadAssetBundleManifestTimeout);
            var bundleManifest = JsonUtility.FromJson<BundleManifest>(json);
            group.Manifest_Remote = new XAssetBundleManifest(bundleManifest);
        }

        private async Task InitGroupFilesHashRemote(VFSGroup group)
        {
            string uri = this.GetWebHashsFileDownloadUrl(this.PlatformText, group.ExtensionGroup, group.GroupName);
            string json = await DownLoadTextFromWebAsync(new Uri(uri), this.DownloadAssetBundleFilesHashTimeout);
            group.FilesHash_Remote = JsonUtility.FromJson<FilesHashBook>(json);
        }

#region Scene 异步加载
        private async UniTask<ISceneAsset> loadSceneAsync(string scenePath)
        {
            if (!mInited)
                throw new VFSException("[TinaX.VFS] Cannot invoke \"load scene\" function before VFS service started. load path:" + scenePath);
            if (QueryAsset(scenePath, out var result, out var group))
            {
                VFSAsset asset;
                //是否已加载
                lock (this)
                {
                    bool load_flag = this.IsAssetLoadedOrLoading(result.AssetPathLower, out asset);
                    if (!load_flag)
                    {
                        asset = new SceneAsset(group, result);
                        asset.LoadTask = doLoadSceneAsync((SceneAsset)asset).ToAsyncLazy();
                        this.Assets.Register(asset);
                    }
                }

                if (asset.LoadState != AssetLoadState.Loaded)
                {
                    await asset.LoadTask;
                }
                asset.Retain();
                return (SceneAsset)asset;
            }
            else
            {
                throw new VFSException((IsChinese ? "被加载的asset的路径是无效的，它不在VFS的管理范围内" : "The asset path you want to load is valid. It is not under the management of VFS") + "Path:" + scenePath, VFSErrorCode.ValidLoadPath);
            }
        }

        private async UniTask doLoadSceneAsync(SceneAsset asset)
        {
            if (asset.LoadState != AssetLoadState.Loaded && asset.LoadState != AssetLoadState.Unloaded) asset.LoadState = AssetLoadState.Loading;
            if (asset.Bundle == null)
            {
                //来加载bundle吧
                asset.Bundle = await loadAssetBundleAndDependenciesAsync(asset.QueryResult.AssetBundleName, asset.Group);
            }
            await asset.LoadAsync();
            //this.Assets.RegisterHashCode(asset); //Scene没有Asset
        }

#endregion

#region VFS Asset 异步加载

        private async UniTask<IAsset> loadAssetAsync(string assetPath, Type type)
        {
            if (!mInited)
                throw new VFSException("[TinaX.VFS] Cannot invoke \"load asset\" function before VFS service started. load path:" + assetPath);
            if (QueryAsset(assetPath, out var result, out var group))
            {
                VFSAsset asset;
                //是否已加载
                lock (this)
                {
                    bool load_flag = this.IsAssetLoadedOrLoading(result.AssetPathLower, out asset);
                    if (!load_flag)
                    {
                        asset = new VFSAsset(group, result);
                        asset.LoadTask = doLoadAssetAsync(asset,type).ToAsyncLazy();
                        this.Assets.Register(asset);
                    }
                    
                }
                if (asset.LoadState != AssetLoadState.Loaded)
                {
                    await asset.LoadTask;
                }
                asset.Retain();
                return asset;
            }
            else
            {
                throw new VFSException((IsChinese ? "被加载的asset的路径是无效的，它不在VFS的管理范围内" : "The asset path you want to load is valid. It is not under the management of VFS") + "Path:" + assetPath, VFSErrorCode.ValidLoadPath);
            }
        }

        private async UniTask doLoadAssetAsync(VFSAsset asset, Type type)
        {
            if (asset.LoadState != AssetLoadState.Loaded && asset.LoadState != AssetLoadState.Unloaded) asset.LoadState = AssetLoadState.Loading;
            if (asset.Bundle == null)
            {
                //来加载bundle吧
                asset.Bundle = await loadAssetBundleAndDependenciesAsync(asset.QueryResult.AssetBundleName, asset.Group);
            }
            await asset.LoadAsync(type);
            this.Assets.RegisterHashCode(asset);
        }
#endregion

#region 加载AssetBundle_Async

        /// <summary>
        /// 加载AssetBundle和它的依赖， 异步入口
        /// </summary>
        /// <param name="assetbundleName"></param>
        /// <param name="counter">引用计数器</param>
        /// <param name="load_chain">加载链：如果是从外部调用的加载，这里为空，如果是递归，则把递归过程中的每一项都加入到加载链</param>
        /// <returns></returns>
        private async UniTask<VFSBundle> loadAssetBundleAndDependenciesAsync(string assetbundleName, VFSGroup group, List<string> load_chain = null)
        {
            VFSBundle bundle;
            bool load_flag;
            //是否已经加载了
            lock (this)
            {
                load_flag = this.Bundles.TryGetBundle(assetbundleName, out bundle);
                if (!load_flag)
                {
                    bundle = new VFSBundle();
                    bundle.AssetBundleName = assetbundleName;
                    bundle.LoadTask = doLoadAssetBundleAndDependenciesAsync(bundle, group, load_chain).ToAsyncLazy();
                    this.Bundles.Register(bundle);
                }
            }

            if (bundle.LoadState != AssetLoadState.Loaded)
                await bundle.LoadTask;

            if (load_flag)
                bundle.RetainWithDependencies();
            else
                bundle.Retain();

            return bundle;
        }

        private async UniTask doLoadAssetBundleAndDependenciesAsync(VFSBundle bundle, VFSGroup group, List<string> load_chain = null)
        {
            //bool dep_self_tag = false; //自依赖 标记 //暂时好像不需要它，给注释掉看看
            List<string> dep_self_list = new List<string>(); //自依赖 列表
            
            //加载链
            if (load_chain == null) load_chain = new List<string>();
            if (!load_chain.Contains(bundle.AssetBundleName)) //这种情况不应该出现，如果有重复加载项应该会被拦在外面一层方法里
                load_chain.Add(bundle.AssetBundleName);
            //依赖
            string[] dependencies;
            this.TryGetDirectDependencies(bundle.AssetBundleName, out dependencies,out _, group);

            bundle.DependenciesNames = dependencies;

            List<UniTask<VFSBundle>> list_dep_load_task = new List<UniTask<VFSBundle>>(); //需要等待的依赖列表
            //加载依赖
            if(dependencies != null && dependencies.Length > 0)
            {
                foreach(var d in dependencies)
                {
                    if (this.TryGetDirectDependencies(d, out var ds, out var dg, group)) 
                    {
                        //自依赖判定
                        bool _dep_self = false;
                        if (load_chain.Contains(d))
                            _dep_self = true;

                        //加载依赖
                        
                        if (!_dep_self)
                        {
                            var task = this.loadAssetBundleAndDependenciesAsync(d, dg, load_chain);
                            list_dep_load_task.Add(task);
                        }
                    }
                }

                if(list_dep_load_task.Count > 0)
                {
                    await UniTask.WhenAll(list_dep_load_task);
                }

                //把依赖对象装入本体
                foreach (var d in dependencies)
                {
                    if(this.Bundles.TryGetBundle(d, out var d_bundle)) //task那个列表不全（可能有自依赖），从那边获取好像更麻烦
                    {
                        if (!bundle.Dependencies.Contains(d_bundle))
                            bundle.Dependencies.Add(d_bundle);
                    }
                }
            }

            //加载本体
            bundle.LoadedPath = this.GetAssetBundleLoadPath(bundle.AssetBundleName,ref group,out string vdisk_path);
            bundle.VirtualDiskPath = vdisk_path;
            bundle.ABLoader = group.ABLoader;
            bundle.GroupHandleMode = group.HandleMode;
            bundle.DownloadTimeout = this.DownloadWebAssetTimeout;

            try
            {
                await bundle.LoadAsync();
            }
            catch
            {
                //如果AB包加载失败，但此时它的依赖项的计数器数字已经+1了，得清理回去
                bundle.Release();
                throw;
            }
        }

#endregion
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
            if(myGroup == null)
            {
                foreach(var group in this.ExtensionGroups.mGroups)
                {
                    if (group.IsAssetPathMatch(path))
                    {
                        myGroup = group;
                        break;
                    }
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
        /// 尝试获取直接依赖
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="dependencies"></param>
        /// <param name="group">被查询依赖的AssetBundle所在的组</param>
        /// <param name="priority">优先查询组：如果不为空，将优先在传入的组中查询</param>
        /// <returns></returns>
        private bool TryGetDirectDependencies(string assetBundleName,out string[] dependencies, out VFSGroup group, VFSGroup priority = null)
        {
            if(priority != null)
            {
                if(priority.AssetBundleManifest.TryGetDirectDependencies(assetBundleName, out dependencies))
                {
                    group = priority;
                    return true;
                }
            }
            foreach(var g in this.mGroups)
            {
                if (priority != null && g == priority) continue;
                if (g.AssetBundleManifest.TryGetDirectDependencies(assetBundleName, out dependencies))
                {
                    group = g;
                    return true;
                }
            }
            
            foreach (var g in this.ExtensionGroups.mGroups)
            {
                if (priority != null && g == priority) continue;
                if (g.AssetBundleManifest.TryGetDirectDependencies(assetBundleName, out dependencies))
                {
                    group = g;
                    return true;
                }
            }
            dependencies = Array.Empty<string>();
            group = null;
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

        private bool IsAssetLoadedOrLoading(string lower_path, out VFSAsset asset)
        {
            return this.Assets.TryGetAsset(lower_path, out asset);
        }

        /// <summary>
        /// AssetBundle 正在加载中或已加载
        /// </summary>
        /// <param name="bundle_name"></param>
        /// <param name="bundle"></param>
        /// <returns></returns>
        private bool IsBundleLoadedOrLoading(string bundle_name,out VFSBundle bundle)
        {
            return Bundles.TryGetBundle(bundle_name, out bundle);
        }

        /// <summary>
        /// 获取资源的加载路径
        /// </summary>
        /// <param name="assetbundle"></param>
        /// <param name="result"></param>
        /// <param name="vdisk_path">这个资源如果保存在vidsk的话，它的路径应该是啥</param>
        /// <returns></returns>
        private string GetAssetBundleLoadPath(string assetbundle, ref VFSGroup group,out string vdisk_path)
        {
            if (group == null) { vdisk_path = string.Empty; return string.Empty; }
            //没有匹配的组，无效
            
            if(group.HandleMode == GroupHandleMode.RemoteOnly)
            {
                //资源只有可能在web,
                vdisk_path = string.Empty;
                return this.GetWebAssetDownloadUrl(PlatformText, assetbundle, ref group);
            }

            //获取vdisk路径
            if(group.ExtensionGroup)
                vdisk_path = ((VFSExtensionGroup)group).GetAssetBundlePath(VirtualDiskPath, assetbundle);
            else
                vdisk_path = group.GetAssetBundlePath(VirtualDiskPath, assetbundle);
            

            if (File.Exists(vdisk_path))
            {
                //资源存在，检查：如果这个资源是LocalOrRemote，并且本地hash与云端不一致的话，则使用云端地址
                if(group.HandleMode == GroupHandleMode.LocalOrRemote && mWebVFSReady)
                {
                    string hash_vdisk = XFile.GetMD5(vdisk_path, true);

                    //尝试找到它在remote的hash
                    if (group.FilesHash_Remote != null)
                    {
                        if(group.FilesHash_Remote.TryGetFileHashValue(assetbundle, out var remote_hash))
                        {
                            if (!hash_vdisk.Equals(remote_hash))
                            {
#if TINAX_DEBUG_DEV
                                Debug.Log($"[VFS]加载AssetBundle {assetbundle} 时，本地Hash({hash_vdisk})与云端Hash{remote_hash}不一致，采用云端下载地址：{this.GetWebAssetDownloadUrl(PlatformText, assetbundle, ref group)}");
#endif
                                //return group.GetAssetBundlePath(VirtualDiskPath, assetbundle);
                                return this.GetWebAssetDownloadUrl(PlatformText, assetbundle, ref group);
                            }
                        }
                    }
                }

                //在上面没有被return 的话，返回vdisk的地址
                return vdisk_path;
            }

            //已知文件不在Virtual Disk
            string asset_path_streamingassets;
            if (group.ExtensionGroup)
                asset_path_streamingassets = ((VFSExtensionGroup)group).GetAssetBundlePath(mStreamingAssets_PackagesRootFolderPath, assetbundle);
            else
                asset_path_streamingassets = group.GetAssetBundlePath(mStreamingAssets_PackagesRootFolderPath, assetbundle);

            //有没有可能这个文件在web?
            if (group.HandleMode == GroupHandleMode.LocalOrRemote && mWebVFSReady)
            {
                if (group.FilesHash_Remote != null && group.FilesHash_StreamingAssets != null)
                {
                    if(group.FilesHash_StreamingAssets.TryGetFileHashValue(assetbundle, out var streamAsset_hash) && group.FilesHash_Remote.TryGetFileHashValue(assetbundle, out var remote_hash))
                    {
                        if(streamAsset_hash.Equals(remote_hash))
                        {
                            return asset_path_streamingassets;
                        }
                    }
                    //return this.GetWebAssetDownloadUrl(PlatformText, assetbundle, ref group);
                }
                else
                    return asset_path_streamingassets; //放弃
            }
            
            return asset_path_streamingassets;
        }

        /// <summary>
        /// 取AssetBundle加载地址，不包含Web地址
        /// </summary>
        /// <param name="assetbundle"></param>
        /// <param name="vdisk_path"></param>
        /// <returns></returns>
        private string GetAssetBundleLoadPathWithoutRemote(string assetbundle,ref VFSGroup group, out string vdisk_path)
        {
            //检查资源是否在Virtual Disk
            if (group == null) { vdisk_path = string.Empty; return string.Empty; }
            if(group.HandleMode == GroupHandleMode.RemoteOnly) { vdisk_path = string.Empty; return string.Empty; }

            if (group.ExtensionGroup)
                vdisk_path = ((VFSExtensionGroup)group).GetAssetBundlePath(VirtualDiskPath, assetbundle);
            else
                vdisk_path = group.GetAssetBundlePath(VirtualDiskPath, assetbundle);

            if (File.Exists(vdisk_path)) return vdisk_path;

            if(group.ExtensionGroup)
                return ((VFSExtensionGroup)group).GetAssetBundlePath(mStreamingAssets_PackagesRootFolderPath, assetbundle);
            else
                return group.GetAssetBundlePath(mStreamingAssets_PackagesRootFolderPath, assetbundle);
        }

        /// <summary>
        /// 获取到完整的，拼接后的，以http://之类的开头的可以直接用的url
        /// </summary>
        /// <param name="platform_name"></param>
        /// <param name="assetBundleName"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        private string GetWebAssetDownloadUrl(string platform_name,string assetBundleName,ref VFSGroup group)
        {
            return this.DownloadWebAssetUrl + this.GetWebAssetUrl(platform_name, assetBundleName, ref group, group.ExtensionGroup);
        }

        /// <summary>
        /// 最终调用 获取完整的Url 下载assetbundle 文件hash
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="isExtensionGroup"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private string GetWebHashsFileDownloadUrl(string platform, bool isExtensionGroup, string groupName)
        {
            return this.DownloadWebAssetUrl + this.GetWebFileHashBookUrl(platform, isExtensionGroup, groupName);
        }

        /// <summary>
        /// 最终调用 获取完整的Url 下载assetbundle manifest
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="isExtensionGroup"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private string GetWebAssetBundleManifestDoanloadUrl(string platform, bool isExtensionGroup, string groupName)
        {
            return this.DownloadWebAssetUrl + this.GetAssetBundleManifestDoanloadUrl(platform, isExtensionGroup, groupName);
        }

        private async UniTask<bool> SayHelloToWebServer(string url, int timeout = 10)
        {
            try
            {
                //Debug.Log("喵，say hello:" + url);
                using (var req = UnityWebRequest.Get(url))
                {
                    req.timeout = timeout;
                    await req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
                        return false;
#else
                if (req.isNetworkError || req.isHttpError)
                    return false;
#endif

                    return (StringHelper.RemoveUTF8BOM(req.downloadHandler.data) == "hello");
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 用于直接从StreamingAssets中加载文件的功能，
        /// 如果传递的路径是一个相对与Assets的路径，比如“Assets/StreamingAssets/xxxx”,
        /// 则把它转化成系统路径返回
        /// 
        /// 如果传递的是其他路径，则认为是相对于StreamingAssets目录的路径，直接拼接上StreamingAssets的根目录然后返回
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string getFilePathForStreamingAssets(string path)
        {
            if (path.StartsWith(_assets_streamingassets_path))
                return Path.Combine(Application.streamingAssetsPath, path.Substring(_assets_streamingassets_path.Length, path.Length - _assets_streamingassets_path.Length));
            else
                return Path.Combine(Application.streamingAssetsPath, path);
        }
        private readonly string _assets_streamingassets_path = "Assets/StreamingAssets/";

#region Customizable_Default_Function
        private string default_getWebAssetUrl(string platform_name, string assetBundleName, ref VFSGroup group, bool isExtensionGroup)
        {
            if (isExtensionGroup)
                return $"{platform_name}/{VFSConst.VFS_FOLDER_EXTENSION}/{VFSUtil.GetExtensionGroupFolderName(group.GroupName)}/{assetBundleName}";
            else
                return $"{platform_name}/{VFSConst.VFS_FOLDER_REMOTE}/{assetBundleName}";
        }

        private string default_getWebFilesHashUrl(string platform_name, bool isExtensionGroup, string groupName)
        {
            if (isExtensionGroup)
                return $"{platform_name}/{VFSConst.VFS_FOLDER_EXTENSION}/{VFSUtil.GetExtensionGroupFolderName(groupName)}/{VFSConst.AssetBundleFilesHash_FileName}";
            else
                return $"{platform_name}/{VFSConst.VFS_FOLDER_DATA}/{VFSConst.MainPackage_AssetBundle_Hash_Files_Folder}/{groupName.GetMD5(true, true)}.json";
        }

        private string default_getAssetBundleManifestDoanloadUrl(string platform_name, bool isExtensionGroup, string groupName)
        {
            if (isExtensionGroup)
                return $"{platform_name}/{VFSConst.VFS_FOLDER_EXTENSION}/{VFSUtil.GetExtensionGroupFolderName(groupName)}/{VFSConst.AssetBundleManifestFileName}";
            else
                return $"{platform_name}/{VFSConst.VFS_FOLDER_DATA}/{VFSConst.MainPackage_AssetBundleManifests_Folder}/{groupName.GetMD5(true, true)}.json";

        }

#endregion


#if UNITY_EDITOR
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
            else
            {
                throw new VFSException((IsChinese ? "被加载的asset的路径是无效的，它不在VFS的管理范围内" : "The asset path you want to load is valid. It is not under the management of VFS")
                                       + "Path:"
                                       + assetPath, VFSErrorCode.ValidLoadPath);
            }

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
            else
            {
                throw new VFSException((IsChinese ? "被加载的Scene的路径是无效的，它不在VFS的管理范围内" : "The Scene path you want to load is valid. It is not under the management of VFS")
                                       + "Path:"
                                       + scenePath, VFSErrorCode.ValidLoadPath);
            }
        }

#endregion

#region 编辑器下的扩展组操作

#endregion

#endif

    }

    /// <summary>
    /// Get download url of Web Assets
    /// </summary>
    /// <param name="platform_name"></param>
    /// <param name="assetBundleName"></param>
    /// <param name="group"></param>
    /// <param name="isExtensionGroup"></param>
    /// <returns></returns>
    public delegate string GetWebAssetDownloadUrlDelegate(string platform_name, string assetBundleName, ref VFSGroup group, bool isExtensionGroup);
    public delegate string GetFileHashDownloadUrlDalegate(string platform_name, bool isExtensionGroup, string groupName);
    public delegate string GetAssetBundleManifestDownloadUrlDalegate(string platform_name, bool isExtensionGroup, string groupName);

}