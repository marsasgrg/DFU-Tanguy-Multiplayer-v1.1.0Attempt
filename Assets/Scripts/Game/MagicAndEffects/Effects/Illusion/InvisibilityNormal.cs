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
    /// Invisibility - Normal
    /// </summary>
    public class InvisibilityNormal : ConcealmentEffect
    {
        public static readonly string EffectKey = "Invisibility-Normal";

        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.ClassicKey = MakeClassicKey(13, 0);
            properties.SupportDuration = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_All;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker | MagicCraftingStations.PotionMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Illusion;
            properties.DurationCosts = MakeEffectCosts(40, 120);
            concealmentFlag = MagicalConcealmentFlags.InvisibleNormal;
            startConcealmentMessageKey = "youAreInvisible";
        }

        public override string GroupName => TextManager.Instance.GetLocalizedText("invisibility");
        public override string SubGroupName => TextManager.Instance.GetLocalizedText("normal");
        public override string DisplayName => string.Format("{0} ({1})", GroupName, SubGroupName);
        public override TextFile.Token[] SpellMakerDescription => DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1560);
        public override TextFile.Token[] SpellBookDescription => DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1260);

        public override void SetPotionProperties()
        {
            PotionRecipe invisibility = new PotionRecipe(
                "invisibility",
                250,
                DefaultEffectSettings(),
                (int)Items.MiscellaneousIngredients1.Rain_water,
                (int)Items.MiscellaneousIngredients1.Nectar,
                (int)Items.CreatureIngredients1.Ectoplasm,
                (int)Items.Gems.Diamond);

            invisibility.TextureRecord = 33;
            AssignPotionRecipes(invisibility);
        }

        protected override bool IsLikeKind(IncumbentEffect other)
        {
            return (other is InvisibilityNormal);
        }
    }
}
