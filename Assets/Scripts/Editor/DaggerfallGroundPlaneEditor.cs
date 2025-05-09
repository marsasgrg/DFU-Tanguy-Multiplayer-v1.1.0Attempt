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
    [CustomEditor(typeof(DaggerfallGroundPlane))]
    public class DaggerfallGroundPlaneEditor : Editor
    {
        private DaggerfallGroundPlane dfGround { get { return target as DaggerfallGroundPlane; } }

        private const string showAboutGroundFoldout = "DaggerfallUnity_ShowAboutGroundFoldout";
        private static bool ShowAboutGroundFoldout
        {
            get { return EditorPrefs.GetBool(showAboutGroundFoldout, true); }
            set { EditorPrefs.SetBool(showAboutGroundFoldout, value); }
        }

        SerializedProperty Prop(string name)
        {
            return serializedObject.FindProperty(name);
        }

        public override void OnInspectorGUI()
        {
            // Update
            serializedObject.Update();

            DisplayAboutGUI();

            // Save modified properties
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private void DisplayAboutGUI()
        {
            EditorGUILayout.Space();
            ShowAboutGroundFoldout = GUILayoutHelper.Foldout(ShowAboutGroundFoldout, new GUIContent("About"), () =>
            {
                GUILayoutHelper.Indent(() =>
                {
                    if (dfGround.Summary.archive == 0)
                    {
                        EditorGUILayout.HelpBox("Ground planes must be created by script or imported using the DaggerfallUnity editor.", MessageType.Info);
                        return;
                    }

                    GUILayoutHelper.Horizontal(() =>
                    {
                        EditorGUILayout.LabelField("Climate", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                        EditorGUILayout.SelectableLabel(dfGround.Summary.climate.ToString(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    });
                    GUILayoutHelper.Horizontal(() =>
                    {
                        EditorGUILayout.LabelField("Season", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                        EditorGUILayout.SelectableLabel(dfGround.Summary.season.ToString(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    });
                });
            });
        }
    }
}