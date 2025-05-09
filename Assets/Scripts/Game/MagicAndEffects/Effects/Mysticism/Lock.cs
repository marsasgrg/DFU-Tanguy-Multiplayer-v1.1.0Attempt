// Project:         Daggerfall Unity
// Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects
{
    /// <summary>
    /// Lock
    /// </summary>
    public class Lock : IncumbentEffect
    {
        public static readonly string EffectKey = "Lock";

        protected int forcedRoundsRemaining = 1;
        protected bool awakeAlert = true;

        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.ClassicKey = MakeClassicKey(16, 255);
            properties.ShowSpellIcon = false;
            properties.SupportChance = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_Self;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Mysticism;
            properties.ChanceCosts = MakeEffectCosts(28, 120, 120);
        }

        public override string GroupName => TextManager.Instance.GetLocalizedText("lock");
        public override TextFile.Token[] SpellMakerDescription => DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1564);
        public override TextFile.Token[] SpellBookDescription => DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1264);

        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);
            StartWaitingForDoor();
        }

        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);
            StartWaitingForDoor();
        }

        protected override int RemoveRound()
        {
            return forcedRoundsRemaining;
        }

        public override int RoundsRemaining
        {
            get { return forcedRoundsRemaining; }
        }

        protected override bool IsLikeKind(IncumbentEffect other)
        {
            return other is Lock;
        }

        protected override void AddState(IncumbentEffect incumbent)
        {
        }

        protected virtual void StartWaitingForDoor()
        {
            // Do nothing if failed
            if (!ChanceSuccess)
            {
                CancelEffect();
                return;
            }

            // Output "Ready to lock." if the host manager is player
            if (awakeAlert && manager.EntityBehaviour == GameManager.Instance.PlayerEntityBehaviour)
            {
                DaggerfallUI.AddHUDText(TextManager.Instance.GetLocalizedText("readyToLock"), 1.5f);
                awakeAlert = false;
            }
        }

        /// <summary>
        /// Called by entity holding Lock incumbent when they activate a door.
        /// For player this is called by PlayerActivate when opening/closing a door.
        /// Enemies cannot use Lock/Open effects at this time.
        /// This effect will automatically close door if open when spell triggered.
        /// </summary>
        /// <param name="actionDoor">DaggerfallActionDoor activated by entity.</param>
        public virtual void TriggerLockEffect(DaggerfallActionDoor actionDoor)
        {
            if (forcedRoundsRemaining == 0)
                return;

            bool activatedByPlayer = (manager.EntityBehaviour == GameManager.Instance.PlayerEntityBehaviour);

            if (actionDoor.IsLocked)
            {
                // Door already locked
                if (activatedByPlayer)
                    DaggerfallUI.AddHUDText(TextManager.Instance.GetLocalizedText("doorAlreadyLocked"), 1.5f);
            }
            else
            {
                // Locks door to level of entity - from spell description "Locks chest or door to lock-level of caster."
                actionDoor.CurrentLockValue = manager.EntityBehaviour.Entity.Level;

                if (activatedByPlayer)
                    DaggerfallUI.AddHUDText(TextManager.Instance.GetLocalizedText("doorLocked"), 1.5f);
            }

            if (actionDoor.IsOpen)
            {
                // Automatically close door if open
                actionDoor.ToggleDoor(activatedByPlayer);
            }

            // Expire effect once door activated
            CancelEffect();
        }

        /// <summary>
        /// Cancel effect.
        /// </summary>
        public void CancelEffect()
        {
            forcedRoundsRemaining = 0;
            ResignAsIncumbent();
        }
    }
}
