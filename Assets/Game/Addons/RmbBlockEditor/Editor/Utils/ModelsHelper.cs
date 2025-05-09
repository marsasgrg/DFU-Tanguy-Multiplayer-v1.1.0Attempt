﻿// Project:         Daggerfall Tools For Unity
// Copyright:       // Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Podleron (podleron@gmail.com)

using System;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Addons.RmbBlockEditor.Elements;
using DaggerfallWorkshop.Utility.AssetInjection;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaggerfallWorkshop.Game.Addons.RmbBlockEditor
{
    public static class ModelsHelper
    {
        public static VisualElement GetPreview(string id)
        {
            var previewObject = RmbBlockHelper.Add3dObject(id);
            return new GoPreview(previewObject);
        }
    }
}