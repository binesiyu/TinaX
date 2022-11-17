﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinaX.VFSKitInternal
{
    public class BundlesManager
    {
        List<VFSBundle> mList_AssetBundles = new List<VFSBundle>();
        Dictionary<string, VFSBundle> mDict_Bundles = new Dictionary<string, VFSBundle>();

        List<VFSBundle> mList_Sync_Temp = new List<VFSBundle>();
        Dictionary<string, VFSBundle> mDict_Sync_Temp = new Dictionary<string, VFSBundle>();

        /// <summary>
        /// 加进来之前执行检查是否已存在，这里不检查！！！
        /// </summary>
        /// <param name="bundle"></param>
        public void Register(VFSBundle bundle)
        {
            mList_AssetBundles.Add(bundle);
            if (mDict_Bundles.ContainsKey(bundle.AssetBundleName))
                mDict_Bundles[bundle.AssetBundleName] = bundle;
            else
                mDict_Bundles.Add(bundle.AssetBundleName, bundle);
        } 

        public void RegisterSyncTemp(VFSBundle bundle)
        {
            mList_Sync_Temp.Add(bundle);
            if (mDict_Sync_Temp.ContainsKey(bundle.AssetBundleName))
                mDict_Sync_Temp[bundle.AssetBundleName] = bundle;
            else
                mDict_Sync_Temp.Add(bundle.AssetBundleName, bundle);
        }

        public bool TryGetBundle(string assetBundleName, out VFSBundle bundle)
        {
            lock (this)
            {
                if (mDict_Bundles.TryGetValue(assetBundleName, out bundle))
                {
                    if (bundle.LoadState == AssetLoadState.Unloaded)
                    {
                        mList_AssetBundles.Remove(bundle);
                        mDict_Bundles.Remove(assetBundleName);
                        bundle = null;
                        return false;
                    }
                    else
                        return true;
                }
                else
                {
                    return false;
                }
            }
        } 

        public bool TryGetBundleSync(string assetBundleName, out VFSBundle bundle)
        {
            lock (this)
            {
                if (mDict_Sync_Temp.TryGetValue(assetBundleName, out bundle))
                {
                    if (bundle.LoadState == AssetLoadState.Unloaded)
                    {
                        mList_Sync_Temp.Remove(bundle);
                        mDict_Sync_Temp.Remove(bundle.AssetBundleName);
                        bundle = null;
                        return false;
                    }
                    else
                        return true;
                }
                else
                    return false;
            }
        }


        public void Refresh()
        {
            for(var i = mList_AssetBundles.Count -1 ; i >= 0; i--)
            {
                if(mList_AssetBundles[i].LoadState == AssetLoadState.Unloaded)
                {
                    mDict_Bundles.Remove(mList_AssetBundles[i].AssetBundleName);
                    mList_AssetBundles.RemoveAt(i);
                }
            }
        }

        public List<VFSBundle> GetVFSBundles()
        {
            var list = new List<VFSBundle>();
            list.AddRange(mList_AssetBundles);
            list.AddRange(mList_Sync_Temp);
            return list;
        }
    }
}
