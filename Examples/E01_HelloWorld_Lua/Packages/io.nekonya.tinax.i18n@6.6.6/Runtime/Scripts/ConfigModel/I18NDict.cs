﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TinaX.I18N.Internal
{
    [CreateAssetMenu(fileName = "i18n_dict", menuName = "TinaX/I18N/I18N Dict")]
    public class I18NDict : ScriptableObject
    {
        public List<I18NKV> data;
    }
}
