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
using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.InternalTypes;
using DaggerfallConnect.Utility;

namespace DaggerfallWorkshop
{
    [CustomEditor(typeof(DaggerfallSongPlayer))]
    public class DaggerfallSongPlayerEditor : Editor
    {
        private DaggerfallSongPlayer dfSongPlayer { get { return target as DaggerfallSongPlayer; } }

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

            if (Application.isPlaying)
            {
                GUILayoutHelper.Horizontal(() =>
                {
                    if (GUILayout.Button("Play"))
                    {
                        dfSongPlayer.Play();
                    }
                    if (GUILayout.Button("Stop"))
                    {
                        dfSongPlayer.Stop();
                    }
                });
            }
        }
    }
}