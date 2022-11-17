using UnityEngine;

namespace TinaXEditor.VFSKitInternal.I18N
{
    internal static class VFSConfigDashboardI18N
    {

        private static bool? _isChinese;
        private static bool IsChinese
        {
            get
            {
                if (_isChinese == null)
                {
                    _isChinese = (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified);
                }
                return _isChinese.Value;
            }
        }

        internal static string WindowTitle
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "VFS 面板";
                else
                    return "VFS Dashboard";
            }
        }

        internal static string EnableVFS
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "启用VFS";
                else
                    return "Enable VFS";
            }
        }

        internal static string GlobalVFS_Ignore_ExtName
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "忽略后缀名";
                else
                    return "Ignore extname";
            }
        }

        internal static string GlobalVFS_Ignore_PathItem
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "忽略路径项目";
                else
                    return "Ignore path item";
            }
        }


        internal static string MsgBox_Common_Error
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "不太对劲";
                else
                    return "Oops!";
            }
        }

        internal static string MsgBox_Common_Confirm
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "好吧";
                else
                    return "Okey";
            }
        }

        internal static string MsgBox_Msg_CreateGroupNameIsNull
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "资源组名称无效哦.";
                else
                    return "The asset group name you want to create is not valid.";
            }
        }

        internal static string MsgBox_Msg_CreateGroupNameHasExists
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "要创建的资源组的名称\"{0}\"已经存在咯.";
                else
                    return "The name \"{0}\" of the assets group you want to create already exists.";
            }
        }

        internal static string Groups_Cannot_Be_Null
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "请保留至少一个Group.";
                else
                    return "Please keep at least one group in config.";
            }
        }



        internal static string Groups_Item_Null_Tips
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "当前配置中没有任何资源组信息，请在窗口上方工具栏新建资源组。";
                else
                    return "There is no assets group information in the current configuration. Please create a new in the toolbar above the window.";
            }
        }


        internal static string Window_GroupConfig_Null_Tips
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "选择一个资源组.";
                else
                    return "Select a group.";
            }
        }

        internal static string Window_GroupConfig_Title_GroupName
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "资源组：";
                else
                    return "Asset Group: ";
            }
        }


        internal static string Window_GroupConfig_Title_FolderPaths
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "白名单文件夹路径：";
                else
                    return "Whitelist folder paths: ";
            }
        }

        internal static string Window_GroupConfig_Title_AssetPaths
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "白名单资源路径：";
                else
                    return "Whitelist asset paths: ";
            }
        }

        internal static string Window_GroupConfig_Title_SpecialFolder
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "资源目录特殊构建规则";
                else
                    return "Folder Special Build Rules";
            }
        }

        internal static string Window_GroupConfig_SelectFolder
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "选择文件夹：";
                else
                    return "Select a folder: ";
            }
        }
        internal static string Window_GroupConfig_SelectAsset
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "选择资源：";
                else
                    return "Select a asset: ";
            }
        }

        internal static string Window_GroupConfig_SelectAsset_Error_Select_Meta
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "不可以选择\".meta\"后缀的文件加入VFS名单";
                else
                    return "Can not select a \".meta\" file to add VFS Asset list.";
            }
        }

        internal static string Window_Cannot_delete_internal_config_content
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "由于VFS内部规则，不可以删除该项：{0}";
                else
                    return "The item \"{0}\"cannot be remove because of VFS internal rules.";
            }
        }

        internal static string Window_Cannot_delete_internal_config_title
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "该项不可删除";
                else
                    return "Cannot remove item.";
            }
        }

        internal static string Window_Group_HandleMode
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "资源组 处理模式：";
                else
                    return "Group handle type：";
            }
        }
        
        internal static string Window_Group_ObfuscateDirectoryStructure
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "混淆目录结构";
                else
                    return "Obfuscate Directory Structure";
            }
        }

        internal static string Window_Group_Extension
        {
            get
            {
                if (IsChinese) return "扩展组：";
                return "Extension Group: ";
            }
        }

        internal static string Window_Group_Extensible_Tips
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "扩展组可以不包含在游戏资源中，它拥有独立的版本管理机制。\n扩展组通常用于制作Mod或者DLC之类的扩展内容。";
                else
                    return "Extension groups are allowed not to be included in game resources. They have independent version management mechanism. \nExtension groups are often used to make extended content such as mod or DLC.";
            }
        }

        internal static string Window_Group_IgnoreSubFolder
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "忽略以下子目录：";
                else
                    return "Ignore The Following Subfolders";
            }
        }

        internal static string Window_Group_IgnoreSubFolder_MsgBox_NotSubfolder
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "您选择的文件夹并不是当前组“白名单文件夹”中配置的任何路径的子目录:\n{0}";
                else
                    return "The folder you selected is not a subdirectory of any path configured in the current group \"Whitelist folder paths\":\n{0}";
            }
        }

        internal static string Menu_Build
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "构建资源";
                else
                    return "Build";
            }
        }

        internal static string Menu_Build_BaseAsset
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "完整资源包（母包）";
                else
                    return "Complete assets package";
            }
        }

        internal static string Window_AB_Detail
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "AssetBundle 细节设置";
                else
                    return "AssetBundle detail setting";
            }
        }


        internal static string Window_AB_Extension_Name
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "AssetBundle 文件扩展名：";
                else
                    return "AssetBundle file extension name: ";
            }
        }

        internal static string Window_AB_Extension_Name_Tip_startwithdot
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified)
                    return "后缀名请以点号\".\"开始。";
                else
                    return "Please start with dot \".\" for file extension.";
            }
        }
        
        internal static string Toolbar_FileServer_NotSupport
        {
            get
            {
                if (IsChinese) return "文件服务器不支持";
                return "File Server Not Support";
            }
        }
        
        internal static string Toolbar_FileServer_Running
        {
            get
            {
                if (IsChinese) return "文件服务器已启动";
                return "File Server Running";
            }
        }
        
        internal static string Toolbar_FileServer_Stopped
        {
            get
            {
                if (IsChinese) return "文件服务器未启动";
                return "File Server Stopped";
            }
        }

        internal static string Toolbar_FileServer_OpenUI
        {
            get
            {
                if (IsChinese) return "管理文件服务器";
                return "Manage File Server";
            }
        }
        
        internal static string Toolbar_VersionMgr
        {
            get
            {
                if (IsChinese) return "版本管理器";
                return "Versions Manager";
            }
        }

        internal static string Change_GroupName
        {
            get
            {
                if (IsChinese) return "更改组名";
                return "Modify Group Name";
            }
        }

        internal static string Is_ExtensionGroup
        {
            get
            {
                if (IsChinese) return "当前资源组为扩展组";
                return "The current group is an extension group";
            }
        }

        internal static string Enable_ExtensionGroup
        {
            get
            {
                if (IsChinese) return "切换为扩展组";
                return "Set as extension group";
            }
        }

        internal static string Delete_ExtensionGroup_Msg
        {
            get
            {
                if (IsChinese) return "你要删除的资源组\"{0}\"是一个扩展组.\n因为扩展组是独立管理版本历史记录的，因此删除该扩展组，会导致与之相关的版本分支也被删除，且不可撤销\n如果你只是暂时不需要该组的资源，可以在Profile中设置相关规则。\n与该组相关的版本分支为:\n";
                return "The assets group \"{0} \" you want to delete  is an extension group. \nBecause the extension group manages the version history independently, deleting the extension group will cause the version branch associated with it to be deleted, and it cannot be undone \nIf you only need the assets of this group temporarily, you can set related rules in the profile. \nThe branch associated with this group is: \n";
            }
        }
        
        internal static string Enable_WebVFS
        {
            get
            {
                if (IsChinese) return "启用时初始化Web VFS：";
                return "init Web VFS OnStart:";
            }
        }

        internal static string WebVFS_DefaultDownloadUrl
        {
            get
            {
                if (IsChinese) return "默认的资源下载基础Url:";
                return "Default Download Url:";
            }
        }

        internal static string WebVFS_DefaultDownloadUrl_tips
        {
            get
            {
                if (IsChinese) return "此处设置的下载地址只是作为一个缺省值的存在，更详细的设置请在Profile窗口中设置";
                return "The download address set here only exists as a default value. For more detailed settings, please set it in the profile window";
            }
        }

        internal static string Btn_AssetsLoadModeInEditor
        {
            get
            {
                if (IsChinese) return "编辑器下资源加载方式";
                return "Assets Load Mode In Editor";
            }
        }

        internal static string AssetsLoadModeInEditor_AssetDatabase
        {
            get
            {
                if (IsChinese) return "编辑器方式加载";
                return "Load By Editor";
            }
        }

        internal static string AssetsLoadModeInEditor_Normal
        {
            get
            {
                if (IsChinese) return "StreamingAssets";
                return "StreamingAssets";
            }
        }

        internal static string AssetsLoadModeInEditor_SourcePackages
        {
            get
            {
                if (IsChinese) return "从资源构建结果目录中加载AssetBundle";
                return "Load AssetBundle from the resource build results directory";
            }
        }

        internal static string FileManager
        {
            get
            {
                if (IsChinese) return "已打包资源管理";
                return "Built Files Manager";
            }
        }

    }
}