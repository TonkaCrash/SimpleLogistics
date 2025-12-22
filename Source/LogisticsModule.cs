using KSP.Localization;
using System;
using UnityEngine;
namespace SimpleLogistics
{
    [Serializable]
    public class LogisticsModule : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Simple Logistics"), UI_Toggle(disabledText = "#SimpLog_Unplugged", enabledText = "#SimpLog_Connected")]  // Unplugged or Connected
        public bool _isActive = false;

        [SerializeField]
        public bool IsActive { get { return _isActive; } }

        public void Set(bool status) { _isActive = status; }

        internal void Toggle() { _isActive = !_isActive; }

        public override string ToString()
        {
            return _isActive ? Localizer.Format("#SimpLog_Unplugged") : Localizer.Format("#SimpLog_Connected");
        } // Unplugged or Connected
        public override string GetInfo()
        {
            return Localizer.Format("#SimpLog_GetInfo"); // Simple Logistics Available
        }

        public override void OnStart(PartModule.StartState state)
        {
        }
        // LGG
        public new void Load(ConfigNode node)
        {
            bool b = false;
            if (node.HasValue("isActive") && bool.TryParse(node.GetValue("isActive"), out b))
                Set(b);
            base.Load(node); // LGG
        }

        // LGG
        public new void Save(ConfigNode node)
        {
            node.AddValue("isActive", _isActive);
            base.Save(node); // LGG
        }
    }
}
