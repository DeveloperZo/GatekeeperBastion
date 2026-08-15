using System;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Game
{
    /// <summary>Precomputed wheel state built by <see cref="ArmoryStation"/> — no sim reads in views.</summary>
    public struct ArmoryWheelDisplay
    {
        public int CentreIndex;
        public WheelAttr Centre;
        public string[] SpokeLabels;
        public int[] SpokeRingIndices;
        public int[] SpokeDistances;
        /// <summary>Filled tick count per spoke (0–5). One tick ≈
        /// <see cref="WeaponWheel.FocusAtMax"/>/5 points.</summary>
        public int[] SpokeTicks;
        public string HubTitle;
        public string ProjFireLine;
        public string ProjReloadLine;
        public string BuyButtonLabel;
        public string ReasonLine;
        public bool ShowBuyButton;
        public WeaponId Weapon;
        public WheelAttr BuyAttr;
    }
}
