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


    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SimpleLogistics : MonoBehaviour
    {
        public const string MODID = "SimpleLogisticsUI";
        public const string MODNAME = "Simple Logistics";

        private static SimpleLogistics instance;
        public static SimpleLogistics Instance { get { return instance; } }

        // So many lists...
        private SortedList<string, double> resourcePool;
        private SortedList<string, double> requestPool;
        private SortedList<string, double> vesselSpareSpace;

        private List<PartResource> partResources;

        private bool requested;
        private bool NetworkActive = false;

        private PluginConfiguration config;

        // GUI vars
        private Rect windowRect;
        private int windowId;
        private bool gamePaused;
        private bool GUIhidden;
        private bool GUIactive;
        private bool GUIrefresh;


        private ToolbarControl toolbarControl;

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
            resourcePool = new SortedList<string, double>();
            requestPool = new SortedList<string, double>();
            vesselSpareSpace = new SortedList<string, double>();

            partResources = new List<PartResource>();

            config = PluginConfiguration.CreateForType<SimpleLogistics>();
            config.load();

            windowRect = config.GetValue<Rect>(this.name, new Rect(0, 0, 400, 400));

            windowId = GUIUtility.GetControlID(FocusType.Passive);

            GUIhidden = false;
            gamePaused = false;
            GUIactive = false;
            GUIrefresh = true;

            requested = false;
            CreateLauncher();

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

        private void OnVesselChange(Vessel vessel)
        {
            requestPool.Clear();
            vesselSpareSpace.Clear();
            foreach (Part part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (!requestPool.ContainsKey(resource.info.name))
                    {
                        requestPool.Add(resource.info.name, 0);
                        vesselSpareSpace.Add(resource.info.name, resource.maxAmount - resource.amount);
                    }
                    else
                        vesselSpareSpace[resource.info.name] += resource.maxAmount - resource.amount;
                }
            }
        }

        public void OnSceneLoaded(Scene gameScene, LoadSceneMode mode)
        {
            if (Enum.TryParse<GameScenes>(gameScene.buildIndex.ToString(), out GameScenes scene))
            {
                if (scene == GameScenes.FLIGHT)
                {
                    OnVesselChange(FlightGlobals.ActiveVessel);
                }
            }
            else
            {
                Debug.Log("Simple Logistics Inactive in this scene");
            }
        }

        #endregion

        #region UI Functions
        private void CreateLauncher()
        {
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(OnAppTrue, OnAppFalse,
                ApplicationLauncher.AppScenes.FLIGHT,
                 MODID,
                "SIButton",
                "SimpleLogistics/Textures/simple-logistics-icon",
                "SimpleLogistics/Textures/simple-logistics-icon-toolbar",
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
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.SPLASHED))
            {
                toolbarControl.SetFalse();
            }

            if (GUIrefresh)
            {
                windowRect.height = 0;
                GUIrefresh = false;
            }

            GUI.skin = HighLogic.Skin;
            GUI.backgroundColor = new Color(45f, 145f, 17f, 1f);
            GUI.contentColor = new Color(45f, 145f, 17f, 0.45f);
            windowRect = ClickThruBlocker.GUILayoutWindow(
                windowId,
                windowRect,
                DrawGUI,
                Localizer.Format("#SimpLog_WindowTitle"),
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );
            if (windowRect.Contains(Event.current.mousePosition))
            {
                LockControls();
            }
            else
            {
                UnlockControls();
            }
        }

        private void DrawGUI(int windowId)
        {
            GUILayout.BeginVertical();
            GUI.contentColor = Color.white;
            GUILayout.Label(Localizer.Format("#SimpLog_VesselName", FlightGlobals.ActiveVessel.GetDisplayName()));
            GUILayout.Label(Localizer.Format("#SimpLog_Status", FlightGlobals.ActiveVessel.SituationString));

            bool ableToRequest = false;

            LogisticsModule lm = FlightGlobals.ActiveVessel.FindPartModuleImplementing<LogisticsModule>();
            if (lm != null)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(lm.IsActive ? Localizer.Format("#SimpLog_Connected") : Localizer.Format("#SimpLog_Unplugged")); // "Plugged In" or "Unplugged"	
                GUI.contentColor = lm.IsActive ? Color.green : Color.red;
                GUILayout.FlexibleSpace();
                GUI.contentColor = Color.white;
                if (GUILayout.Button(Localizer.Format("#SimpLog_Toggle"))) // "Toggle Plug"
                {
                    lm.Toggle();
                    GUIrefresh = true;
                }
                GUILayout.FlexibleSpace();
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
                // if plugged in - not able to request
                ableToRequest = !lm.IsActive;
            }

            GetVesselSpareSpace();

            GUI.contentColor = Color.yellow;
            GUILayout.Label(Localizer.Format("#SimpLog_Pool")); //"Resource Pool:"

            foreach (var resource in resourcePool)
            {
                GUILayout.BeginHorizontal();
                GUI.contentColor = Color.yellow;
                GUILayout.Label(resource.Key, GUILayout.Width(170));
                GUI.contentColor = Color.white;

                if (ableToRequest && requestPool.ContainsKey(resource.Key))
                {
                    GUILayout.Label(requestPool[resource.Key].ToString("0.00") + " / " +
                        resource.Value.ToString("0.00"));
                }
                else
                    GUILayout.Label(resource.Value.ToString("0.00"));
                GUILayout.EndHorizontal();

                if (ableToRequest && requestPool.ContainsKey(resource.Key))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("0", GUILayout.Width(20)))
                        requestPool[resource.Key] = 0;
                    requestPool[resource.Key] = GUILayout.HorizontalSlider((float)requestPool[resource.Key], 0, (float)Math.Min(vesselSpareSpace[resource.Key], resource.Value),
                        GUILayout.Width(150)
                    );

                    if (GUILayout.Button(vesselSpareSpace[resource.Key].ToString("0.00")))
                        requestPool[resource.Key] = Math.Min(vesselSpareSpace[resource.Key], resource.Value);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (ableToRequest)
                if (GUILayout.Button(Localizer.Format("#SimpLog_Request"))) // "Request Resources"
                    requested = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.SPLASHED))

            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#SimpLog_ErrorMessage"));
                return;
            }

            GUIactive = true;
        }

        public void OnAppFalse()
        {
            GUIactive = false;
            GUIrefresh = true;
            UnlockControls();
        }

        internal virtual void OnToggle()
        {
            if ((FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED) &&
                (FlightGlobals.ActiveVessel.situation != Vessel.Situations.SPLASHED))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#SimpLog_ErrorMessage"));
                return;
            }

            GUIactive = !GUIactive;
            if (!GUIactive)
            {
                GUIrefresh = true;
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
            // Find all resources in the network
            partResources.Clear();
            NetworkActive = false; // turn off network
            foreach (Vessel vessel in FlightGlobals.VesselsLoaded)
            {
                if ((vessel.situation != Vessel.Situations.LANDED) &&
                    (vessel.situation != Vessel.Situations.SPLASHED))
                    continue;

                LogisticsModule lm = vessel.FindPartModuleImplementing<LogisticsModule>();
                if (lm != null && !lm.IsActive)
                    continue;

                foreach (Part part in vessel.parts)
                {
                    if (part.State == PartStates.DEAD)
                        continue;

                    foreach (PartResource resource in part.Resources)
                    {
                        if (resource.info.resourceTransferMode == ResourceTransferMode.NONE ||
                            resource._flowMode == PartResource.FlowMode.None ||
                            !resource._flowState)
                            continue;

                        NetworkActive = true; // Got through checks above, so network is active
                        partResources.Add(resource);
                    }
                }
            }

            if (NetworkActive)  // Skip further processing if nothing is active
            {
                // Create a resource pool
                resourcePool.Clear();
                foreach (var resource in partResources)
                {
                    if (!resourcePool.ContainsKey(resource.info.name))
                        resourcePool.Add(resource.info.name, resource.amount);
                    else
                        resourcePool[resource.info.name] += resource.amount;
                }

                // Spread resources evenly
                foreach (var resource in resourcePool)
                {
                    double value = resource.Value;
                    if (requested)
                    {
                        if (requestPool.ContainsKey(resource.Key))
                        {
                            value -= requestPool[resource.Key];
                        }
                    }

                    var resList = partResources.FindAll(r => r.info.name == resource.Key);

                    ShareResource(resList, value);
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
        private void ShareResource(List<PartResource> resources, double amount)
        {
            // Portion each may potentially receive
            double portion = amount / resources.Count;

            // Those who may not grab whole portion
            var minors = resources.FindAll(r => r.maxAmount < portion);

            // Those who may grab whole portion and even ask for more :D
            var majors = resources.FindAll(r => r.maxAmount >= portion);

            if (minors.Count > 0)
            {
                // Some may not handle this much
                foreach (var minor in minors)
                {
                    minor.amount = minor.maxAmount;
                    amount -= minor.maxAmount;
                }
                // Love recursion :D
                if (amount > 0)
                    ShareResource(majors, amount);
            }
            else
            {
                // Portion size is good for everybody
                foreach (var major in majors)
                {
                    major.amount = portion;
                }
            }
        }

        /// <summary>
        /// Get the amount of spare resource space. Calling every physics frame is stupid, but who cares :D
        /// </summary>
        private void GetVesselSpareSpace()
        {
            vesselSpareSpace.Clear();
            foreach (Part part in FlightGlobals.ActiveVessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (!vesselSpareSpace.ContainsKey(resource.info.name))
                        vesselSpareSpace.Add(resource.info.name, resource.maxAmount - resource.amount);
                    else
                        vesselSpareSpace[resource.info.name] += resource.maxAmount - resource.amount;
                }
            }
        }
        private static bool onetime = true;
        // Code duplication? No way!
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
                    if (onetime) Debug.Log("SimpleLogistics - count" + AvailablePool.Count.ToString() + " Key " + resource.Key);
                    double value = resource.Value;
                    var resList = AvailableResources.FindAll(r => r.info.name == resource.Key);
                    value += requestPool[resource.Key];
                    requestPool[resource.Key] = 0;
                    if (onetime) Debug.Log("SimpleLogistics - End Keys " + resource.Key);
                    ShareResource(resList, value);
                }
                catch
                {
                    Debug.Log("SimpleLogistivs - Exc" + resource.Key);
                }
            }
            onetime = false;
        }
        #endregion
    }
}

