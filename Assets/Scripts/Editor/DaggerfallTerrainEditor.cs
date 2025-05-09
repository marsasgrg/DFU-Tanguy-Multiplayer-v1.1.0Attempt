﻿// Project:         Daggerfall Unity
// Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DaggerfallConnect.Arena2;

namespace DaggerfallWorkshop
{
    [CustomEditor(typeof(DaggerfallTerrain))]
    public class DaggerfallTerrainEditor : Editor
    {
        private DaggerfallTerrain dfTerrain { get { return target as DaggerfallTerrain; } }

        SerializedProperty Prop(string name)
        {
            return serializedObject.FindProperty(name);
        }

        public override void OnInspectorGUI()
        {
            // Update
            serializedObject.Update();

            DisplayGUI();

            // Save modified properties
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private void DisplayGUI()
        {
            DrawDefaultInspector();

            //if (GUILayout.Button("Update Terrain"))
            //{
            //    dfTerrain.__EditorUpdateTerrain();
            //}
        }
    }
}