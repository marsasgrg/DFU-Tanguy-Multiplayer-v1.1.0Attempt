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

using System.Text.RegularExpressions;
using FullSerializer;

namespace DaggerfallWorkshop.Game.Questing
{
    /// <summary>
    /// Incomplete. Just stubbing out action for now so quest will compile.
    /// </summary>
    public class DropAsQuestor : ActionTemplate
    {
        Symbol target;

        public override string Pattern
        {
            get { return @"drop (?<target>[a-zA-Z0-9_.-]+) as questor"; }
        }

        public DropAsQuestor(Quest parentQuest)
            : base(parentQuest)
        {
        }

        public override IQuestAction CreateNew(string source, Quest parentQuest)
        {
            // Source must match pattern
            Match match = Test(source);
            if (!match.Success)
                return null;

            // Factory new action
            DropAsQuestor action = new DropAsQuestor(parentQuest);
            action.target = new Symbol(match.Groups["target"].Value);

            return action;
        }

        public override void Update(Task caller)
        {
            // Drop questor
            ParentQuest.DropQuestor(target);

            SetComplete();
        }

        #region Serialization

        [fsObject("v1")]
        public struct SaveData_v1
        {
            public Symbol target;
        }

        public override object GetSaveData()
        {
            SaveData_v1 data = new SaveData_v1();
            data.target = target;

            return data;
        }

        public override void RestoreSaveData(object dataIn)
        {
            if (dataIn == null)
                return;

            SaveData_v1 data = (SaveData_v1)dataIn;
            target = data.target;
        }

        #endregion
    }
}