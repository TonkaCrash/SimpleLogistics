using ClickThroughFix;
using KSP.IO;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleLogistics
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(SimpleLogistics.MODID, SimpleLogistics.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SimpleLogistics : MonoBehaviour
    {
        public const string MODID = "SimpleLogisticsUI";
        public const string MODNAME = "Simple Logistics";

        private static SimpleLogistics instance;
        public static SimpleLogistics Instance { get { return instance; } }

        // Network Pool
        private bool NetworkActive = false;
        private int NetworkCount = 0;
        private SortedList<string, double> resourcePool;
        private SortedList<string, double> emptySpacePool;

        // Vessel Inventory
        private SortedList<string, double> vesselAmount;
        private SortedList<string, double> vesselMaxAmount;
        private SortedList<string, double> vesselRequest;

        private List<PartResource> partResources;

        // Request for resources 
        private bool requested;

        private PluginConfiguration config;

        // GUI vars
        private Rect windowRect;
        private int windowId;
        private bool gamePaused;
        private bool GUIhidden;
        private bool GUIactive;

        private ToolbarControl toolbarControl;
        private GUIStyle ButtonStyle;

        // Same as Debug Toolbar lock mask
        private const ulong lockMask = 900719925474097919;

        #region Primary Functions
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            // Network state
            resourcePool = new SortedList<string, double>();
            emptySpacePool = new SortedList<string, double>();

            // Vessel State
            vesselAmount = new SortedList<string, double>();
            vesselMaxAmount = new SortedList<string, double>();
            vesselRequest = new SortedList<string, double>();

            partResources = new List<PartResource>();

            config = PluginConfiguration.CreateForType<SimpleLogistics>();
            config.load();

            windowRect = config.GetValue<Rect>(this.name, new Rect(0, 0, 200, 200));
            windowId = GUIUtility.GetControlID(FocusType.Passive);

            GUIhidden = false;
            gamePaused = false;
            GUIactive = false;
            requested = false;

            CreateButtonIcon();

            SceneManager.sceneLoaded += OnSceneLoaded;
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
        }

        private void OnDestroy()
        {
            config.SetValue(this.name, windowRect);
            config.save();

            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onGamePause.Remove(OnGamePause);
            GameEvents.onGameUnpause.Remove(OnGameUnpause);

            UnlockControls();
            DestroyLauncher();

            if (instance == this) instance = null;
        }
        public void OnSceneLoaded(Scene gameScene, LoadSceneMode mode)
        {
            if (Enum.TryParse<GameScenes>(gameScene.buildIndex.ToString(), out GameScenes scene))
                if (scene == GameScenes.FLIGHT)
                {
                    OnVesselChange(FlightGlobals.ActiveVessel);
                    InventoryNetwork();
                }
                else
                    Debug.Log("Simple Logistics Inactive in this scene");
        }
        private void OnVesselChange(Vessel vessel)
        {
            InventoryVessel(vessel);
        }

        #endregion

        #region UI Functions
        private void CreateButtonIcon()
        {
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(OnAppTrue, OnAppFalse,
                ApplicationLauncher.AppScenes.FLIGHT,
                 MODID,
                "SimpleLogisticsButton",
                "SimpleLogistics/Textures/SLicon_38",
                "SimpleLogistics/Textures/SLicon_24",
                MODNAME
            );
        }

        public void DestroyLauncher()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
            }
        }

        public void OnGUI()
        {
            if (gamePaused || GUIhidden || !GUIactive) return;

            if ((FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.SPLASHED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.PRELAUNCH))
            {
                toolbarControl.SetFalse();
            }

            GUI.skin = HighLogic.Skin;

/*            ButtonStyle = new GUIStyle(HighLogic.Skin.button)
            {
                normal = { textColor = Color.white },
                fontSize = 18, // (int)(10 * GuiDisplaySize.Offset),
                margin = new RectOffset(0, 0, 5, 5),
                fixedHeight = ButtonStyle.fontSize,
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(),
                alignment = TextAnchor.MiddleCenter,
                fontSize = (int)(11 * GuiDisplaySize.Offset),
                fixedHeight = 18.0f * GuiDisplaySize.Offset
            };

            CompactButtonStyle = new GUIStyle(ButtonStyle)
            {
                fontSize = (int)(10 * GuiDisplaySize.Offset),
                margin = new RectOffset(0, 0, 5, 5),
                fixedHeight = ButtonStyle.fontSize
            };
*/
            windowRect = ClickThruBlocker.GUILayoutWindow(
                windowId,
                windowRect,
                DrawGUI,
                Localizer.Format("#SimpLog_WindowTitle"),
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );
            if (windowRect.Contains(Event.current.mousePosition))
                LockControls();
            else
                UnlockControls();
        }

        private void DrawGUI(int windowId)
        {
            windowRect.height = 200;
            GUILayout.BeginVertical();

            GUI.color = Color.white;
            GUILayout.Label("<b>" + Localizer.Format("#SimpLog_VesselName", FlightGlobals.ActiveVessel.GetDisplayName()) + "</b>");
            if (NetworkActive)
            {
                if (NetworkCount == 1)
                    GUILayout.Label("Network Active: Only One Vessel in Network");
                else
                    GUILayout.Label("Network Active: " + NetworkCount.ToString() + " Vessels in Network Active");
            }
            else
            { 
                GUILayout.Label("No Vessels in Network");
            }
    

            bool ableToRequest = false;

            LogisticsModule lm = FlightGlobals.ActiveVessel.FindPartModuleImplementing<LogisticsModule>();
            if (lm != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localizer.Format("#SimpLog_Toggle"), GUILayout.Width(150))) // "Toggle Plug"
                    lm.Toggle();
                GUI.contentColor = lm.IsActive ? Color.green : Color.white;
                GUILayout.FlexibleSpace();

                GUILayout.Label(lm.IsActive ? Localizer.Format("#SimpLog_Connected") : Localizer.Format("#SimpLog_Unplugged")); // "Plugged In" or "Unplugged"	
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                // if plugged in - not able to request
                ableToRequest = !lm.IsActive && NetworkActive;
            }
            GUI.contentColor = Color.white;

            if (NetworkActive)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#SimpLog_Pool"), GUILayout.Width(150)); //"Resource Pool:"
                GUILayout.FlexibleSpace();
                GUILayout.Label("<b>Total</b>", GUILayout.Width(75)); 
                GUILayout.EndHorizontal();

                foreach (var resource in resourcePool)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.Key, GUILayout.Width(150));
                    if (ableToRequest && vesselRequest.ContainsKey(resource.Key))
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(vesselRequest[resource.Key].ToString("0.0") + " / " + resource.Value.ToString("0.00"));
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(vesselMaxAmount[resource.Key].ToString("0.0"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        double amount = vesselMaxAmount[resource.Key] - vesselAmount[resource.Key];
                        if (GUILayout.Button("0.0", GUILayout.Width(10)))
                            vesselRequest[resource.Key] = 0;
                        vesselRequest[resource.Key] = GUILayout.HorizontalSlider(
                            (float)vesselRequest[resource.Key], 0, 
                            (float)amount, 
                            GUILayout.Width(200));
                        if (GUILayout.Button(amount.ToString("0.0")))
                            vesselRequest[resource.Key] = amount;
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(resource.Value.ToString("0.0"), GUILayout.Width(75));
                    }
                    GUILayout.EndHorizontal();
                }
                if (ableToRequest)
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Localizer.Format("#SimpLog_Request"))) // "Request Resources"
                        requested = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("<b>Vessel Resources</b>"), GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                GUILayout.Label(Localizer.Format("<b>Amount</b>"), GUILayout.Width(75));
                GUILayout.FlexibleSpace();
                GUILayout.Label(Localizer.Format("<b>Max</b>"), GUILayout.Width(75));
                GUILayout.EndHorizontal();

                foreach (var resource in vesselAmount)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.Key, GUILayout.Width(150));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(vesselAmount[resource.Key].ToString("0.0"), GUILayout.Width(75));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(vesselMaxAmount[resource.Key].ToString("0.0"), GUILayout.Width(75));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.contentColor = Color.red;
            if (GUILayout.Button(Localizer.Format("#SimpLog_Close")))
                toolbarControl.SetFalse();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        public void OnGamePause()
        {
            gamePaused = true;
            UnlockControls();
        }

        public void OnGameUnpause()
        {
            gamePaused = false;
        }

        private void OnHideUI()
        {
            GUIhidden = true;
            UnlockControls();
        }

        private void OnShowUI()
        {
            GUIhidden = false;
        }

        public void OnAppTrue()
        {
            if ((FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.SPLASHED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.PRELAUNCH))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#SimpLog_ErrorMsg"));
                return;
            }
            GUIactive = true;
        }

        public void OnAppFalse()
        {
            GUIactive = false;
            UnlockControls();
        }

        internal virtual void OnToggle()
        {
            if ((FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.SPLASHED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.PRELAUNCH))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#SimpLog_ErrorMsg"));
                return;
            }

            GUIactive = !GUIactive;
            if (!GUIactive)
            {
                UnlockControls();
            }
        }

        private ControlTypes LockControls()
        {
            return InputLockManager.SetControlLock((ControlTypes)lockMask, this.name);
        }

        private void UnlockControls()
        {
            InputLockManager.RemoveControlLock(this.name);
        }
        #endregion

        #region Resource Sharing
        private void FixedUpdate()
        {
            InventoryVessel(FlightGlobals.ActiveVessel);
            InventoryNetwork();

            if (NetworkActive)  // Skip processing if nothing is active
            {
                // Distribute resources
                foreach (var resource in resourcePool)
                {
                    var resList = partResources.FindAll(r => r.info.name == resource.Key);
                    double amount = resource.Value;
                    double empty = emptySpacePool[resource.Key];
                    // remove requests from pool
                    if (requested && vesselRequest.ContainsKey(resource.Key))
                    {
                        amount -= vesselRequest[resource.Key];
                        empty += vesselRequest[resource.Key];
                    }
                    ShareResource(resList, amount, empty);
                }
                if (requested)
                {
                    TransferResources();
                    requested = false;
                }
            }
        }

        /// <summary>
        /// Distributes resource evenly across every capacitor with priority to low capacity first
        /// </summary>
        /// <param name="resources">List of resources</param>
        /// <param name="amount">Overall amount</param>
        private void ShareResource(List<PartResource> resources, double amount, double empty)
        {
            // Fix for resource converter adding resources to pool. Calculate an average spare space (max 5 units)
            // that is left in all parts and let tanks fill. 
            double spare = empty / resources.Count;
            //if (spare < 1.0f) spare = 0.0f;
            if (spare > 5.0f) spare = 5.0f;

            // Portion each may potentially receive
            double portion = amount / resources.Count;

            // Those who may not grab whole portion
            var minors = resources.FindAll(r => r.maxAmount < Math.Max(0.0f, portion - spare));

            // Those who can grab more than the average portion
            var majors = resources.FindAll(r => r.maxAmount >= portion);

            if (minors.Count > 0)
            {
                // small tanks that are near full
                foreach (var minor in minors)
                {
                    minor.amount = Math.Max(0.0f, minor.maxAmount - spare);
                    amount -= minor.amount;
                    empty -= spare;
                }
                // Big tanks that should take full portion
                ShareResource(majors, amount, empty);
            }
            else
            {
                // Portion size is good for everybody which means everybody is either same size or mostly empty.
                foreach (var major in majors)
                    major.amount = portion;
            }
        }

        // private static bool onetime = true;
        private void TransferResources()
        {
            List<PartResource> AvailableResources = new List<PartResource>();
            SortedList<string, double> AvailablePool = new SortedList<string, double>();

            foreach (Part part in FlightGlobals.ActiveVessel.parts)
            {
                if (part.State == PartStates.DEAD)
                    continue;

                foreach (PartResource resource in part.Resources)
                {
                    if (resource.info.resourceTransferMode == ResourceTransferMode.NONE ||
                        resource._flowMode == PartResource.FlowMode.None ||
                        !resource._flowState)
                        continue;

                    AvailableResources.Add(resource);
                }
            }

            foreach (var resource in AvailableResources)
            {
                if (!AvailablePool.ContainsKey(resource.info.name))
                    AvailablePool.Add(resource.info.name, resource.amount);
                else
                    AvailablePool[resource.info.name] += resource.amount;
            }
            // Spread resources evenly
            foreach (var resource in AvailablePool)
            {
                try
                {
                    double value = resource.Value;
                    var resList = AvailableResources.FindAll(r => r.info.name == resource.Key);
                    value += vesselRequest[resource.Key];
                    vesselRequest[resource.Key] = 0;
                    double spareSpace = 0f;
                    ShareResource(resList, value, spareSpace);
                }
                catch
                {
                    Debug.Log("SimpleLogistivs - Exc" + resource.Key);
                }
            }
        }
        #endregion
        private void InventoryVessel(Vessel vessel)
        {   // vessel lists use common keys
            vesselRequest.Clear();
            vesselAmount.Clear();
            vesselMaxAmount.Clear();
            foreach (Part part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (vesselRequest.ContainsKey(resource.info.name))
                    {
                        vesselAmount[resource.info.name] += resource.amount;
                        vesselMaxAmount[resource.info.name] += resource.maxAmount;
                    }
                    else
                    {
                        vesselAmount.Add(resource.info.name, resource.amount);
                        vesselMaxAmount.Add(resource.info.name, resource.maxAmount);
                        vesselRequest.Add(resource.info.name, 0);

                    }
                }
            }
        }
        private void InventoryNetwork()
        {
            emptySpacePool.Clear();
            resourcePool.Clear();
            partResources.Clear();
            NetworkCount = 0;
            NetworkActive = false; // turn off network
            foreach (Vessel vessel in FlightGlobals.VesselsLoaded)
            {   // situation valid
                if ((vessel.situation != Vessel.Situations.LANDED) &&
                    (vessel.situation != Vessel.Situations.SPLASHED) &&
                    (vessel.situation != Vessel.Situations.PRELAUNCH))
                    continue;
                // module activated
                LogisticsModule lm = vessel.FindPartModuleImplementing<LogisticsModule>();
                if (lm != null && !lm.IsActive)
                    continue;

                // resource and parts allow transfer
                foreach (Part part in vessel.parts)
                {
                    if (part.State == PartStates.DEAD)
                        continue;
                    foreach (PartResource resource in part.Resources)
                    {
                        if (resource.info.resourceTransferMode == ResourceTransferMode.NONE ||
                            resource.info.resourceFlowMode == ResourceFlowMode.NO_FLOW ||
                            resource._flowMode == PartResource.FlowMode.None || !resource._flowState)
                            continue;
                        partResources.Add(resource);
                    }
                }
                NetworkCount++;
                NetworkActive = true; // Got through checks above, so network is active
            }

            if (NetworkActive)
            {
                foreach (var resource in partResources)
                {
                    if (!resourcePool.ContainsKey(resource.info.name))
                    {
                        resourcePool.Add(resource.info.name, resource.amount);
                        emptySpacePool.Add(resource.info.name, resource.maxAmount - resource.amount);
                    }
                    else
                    {
                        resourcePool[resource.info.name] += resource.amount;
                        emptySpacePool[resource.info.name] += resource.maxAmount - resource.amount;
                    }
                }
            }
        }
    }

}

