// Copyright (c) 2025 PinePie. All rights reserved.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PinePie.PieTabs
{
    [InitializeOnLoad]
    public static class NavigatorLoader
    {
        static NavigatorLoader() => EditorApplication.update += RunOnceOnLoad;

        static void RunOnceOnLoad()
        {
            EditorApplication.update -= RunOnceOnLoad;

            if (!Directory.Exists($"{PathUtility.GetPieTabsPath()}/PinePie/PieTabs"))
                return;

            Navigator.Setup();
            UI.mainUI.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                Selection.selectionChanged -= EnsurePieDeskOverlay;

                EditorApplication.delayCall += () =>
                {
                    var projectWindow = Navigator.GetProjectBrowserUI();
                    if (UI.mainUI.panel == null)
                    {
                        Selection.selectionChanged += EnsurePieDeskOverlay;
                    }
                };
            });
        }

        private static void EnsurePieDeskOverlay()
        {
            var lastFocused = EditorWindow.focusedWindow;
            if (lastFocused != null && lastFocused.GetType().Name == "ProjectBrowser")
            {
                Selection.selectionChanged -= EnsurePieDeskOverlay;
                Navigator.Setup();
            }
        }

        [MenuItem("Tools/Refresh PieTabs")]
        public static void RefreshPieDesk()
        {
            Navigator.Setup();
        }
    }

    public static partial class Navigator
    {
        public static ShortcutButtonBundle navButtons = new();
        public static CreatorButtonBundle creatorButtons = new();

        private const string SplitterKey = "PieTabs_LastSplitterSpacing";
        private const string SearchBarOpenKey = "PieTabs_SearchBarOpen";

        private static float LastSplitterSpacing
        {
            get => EditorPrefs.GetFloat(SplitterKey, 300f);
            set => EditorPrefs.SetFloat(SplitterKey, value);
        }

        private static bool IsSearchBarOpen
        {
            get => EditorPrefs.GetBool(SearchBarOpenKey, false);
            set => EditorPrefs.SetBool(SearchBarOpenKey, value);
        }

        private static int placeHolderIndex;


        public static void Setup()
        {
            UI.projectBrowserUI = GetProjectBrowserUI();
            UI.mainUI = LoadUXML("PieDeskMainUI.uxml").Instantiate().Q<VisualElement>("PieDeskUI");
            UI.mainUI.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>($"{PathUtility.GetPieTabsPath()}/PinePie/PieTabs/editor/Core/UI/PieDeskStyling.uss"));

            UI.shortcutButtonAsset = LoadUXML("shortcutButton.uxml");
            UI.creatorButtonAsset = LoadUXML("creatorButton.uxml");
            UI.placeholderNeedle = LoadUXML("placeholderLine.uxml").Instantiate().Q<VisualElement>("line");

            UI.copiedText = UI.mainUI.Q<VisualElement>("copiedText");

            UI.colorPopup = UI.mainUI.Q<VisualElement>("colorPopup").Q<VisualElement>("colorPopup");


            UI.projectBrowserUI.Clear();

            UI.shortcutButtonArea = UI.mainUI.Q<ScrollView>("shorcutsDragArea");
            UI.creatorButtonArea = UI.mainUI.Q<ScrollView>("CreationMenuDragArea");
            if (!IsTwoColumnMode())
            {
                UI.shortcutButtonArea.style.display = DisplayStyle.None;
                UI.creatorButtonArea.style.flexGrow = 1;

                UI.mainUI.Q<VisualElement>("splitter").style.display = DisplayStyle.None;

                VisualElement bottomBar = UI.mainUI.Q<VisualElement>("bottomAddressBar");
                bottomBar.style.marginRight = 0;
                bottomBar.style.marginLeft = 0;

                UI.shortcutButtonArea = UI.mainUI.Q<ScrollView>("CreationMenuDragArea");
            }
            else // two coloumn mode 
            {
                SetupSplitter();
                SetupBottomBarMargin();
                UI.creatorButtonArea.style.width = LastSplitterSpacing;

                // asset creator button 
                creatorButtons.LoadFromJson(UI.creatorButtonAsset);
                SetupDragNDropForCreatorArea();
                FillCreatorButtons();
            }

            CallbacksForPopupBoxes();
            RegisterAddressCopyCallbacks();
            SetupDragAreaStyling();
            SetupSearchBarControls();
            CallbacksForColorPopup();

            // shortcut buttons
            navButtons.LoadFromJson(UI.shortcutButtonAsset);
            SetupDragNDropForShortcutArea(UI.shortcutButtonArea, navButtons);
            FillShortcutButtons();

            UI.projectBrowserUI.Add(UI.mainUI);
        }


        // click callbacks
        public static void OnShortcutButtonClicked(
            VisualElement UIbutton,
            Action<VisualElement> depSelection,
            ShortcutButton buttonProp)
        {
            // callbacks
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(buttonProp.Path);
            UIbutton.AddManipulator(new ShortcutDragManipulator(obj));

            UIbutton.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    UIbutton.Q<VisualElement>("shade").style.backgroundColor = new Color(255, 255, 255, 0.15f);

                    evt.StopPropagation();
                }
                else if (evt.button == 1)
                {
                    evt.StopPropagation();
                }
            });

            UIbutton.RegisterCallback<PointerUpEvent>(evt =>
            {
                // single click
                if (evt.button == 0)
                {
                    if (obj == null) return;

                    if (evt.ctrlKey)
                    {
                        depSelection?.Invoke(UIbutton);

                        if (AssetDatabase.IsValidFolder(buttonProp.Path))
                            OpenFolder(buttonProp.Path);
                        else
                            AssetDatabase.OpenAsset(obj);


                        evt.StopPropagation();
                    }
                    else if (evt.shiftKey)
                    {
                        buttonProp.isMinimal = !buttonProp.isMinimal;
                        UIbutton.Q<Label>("buttonLabel").text = buttonProp.isMinimal ? "" : buttonProp.Label;
                        UIbutton.tooltip = buttonProp.isMinimal ? buttonProp.Label : null;

                        var buttonShade = UIbutton.Q<VisualElement>("shade");
                        buttonShade.Q<Label>("buttonLabel").text = buttonProp.isMinimal ? "" : buttonProp.Label;

                        navButtons.SaveToJson();

                        evt.StopPropagation();
                    }
                    else if (evt.altKey)
                    {
                        ShowBoxAtPos(UI.colorPopup, evt.position.x - 100);

                        ColorPopup.isForCreator = false;
                        ColorPopup.popupIsOpen = true;

                        ColorPopup.activeNavButton = buttonProp;
                        ColorPopup.activeVisualItem = UIbutton;

                        evt.StopPropagation();
                    }
                    else
                    {
                        depSelection?.Invoke(UIbutton);

                        // uncomment line below to open folder even in single click

                        // if (AssetDatabase.IsValidFolder(buttonProp.Path))
                        //     OpenFolder(buttonProp.Path);
                        // else
                        FocusAsset(buttonProp.Path);

                        evt.StopPropagation();
                    }
                }
                else if (evt.button == 1)
                {
                    RemoveButton(buttonProp);

                    evt.StopPropagation();
                }

                UIbutton.Q<VisualElement>("shade").style.backgroundColor = new Color(255, 255, 255, 0.12f);
            });

        }

        public static void OnAssetCreatorButtonClicked(
            VisualElement UIbutton,
            Action<VisualElement> depSelection,
            CreatorButton buttonProp)
        {
            // callbacks
            UIbutton.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    UIbutton.Q<VisualElement>("shade").style.backgroundColor = new Color(255, 255, 255, 0.15f);

                    evt.StopPropagation();
                }
                else if (evt.button == 1)
                {
                    evt.StopPropagation();
                }
            });

            UIbutton.RegisterCallback<PointerUpEvent>(evt =>
            {
                // single click
                if (evt.button == 0)
                {
                    if (evt.ctrlKey)
                    {
                        List<string> items = GetAssetCreateMenuEntries();

                        var menu = new GenericMenu();
                        foreach (var item in CleanEntries(items))
                        {
                            var trimmedItem = item;
                            const string prefix = "Assets/Create/";

                            if (item.StartsWith(prefix))
                                trimmedItem = item.Substring(prefix.Length);

                            menu.AddItem(new GUIContent(trimmedItem), false, () =>
                            {
                                OnEditMenuEntry(buttonProp, item);
                            });
                        }
                        menu.DropDown(new Rect(evt.position, Vector2.zero));

                        evt.StopPropagation();
                    }
                    // minimal mode
                    else if (evt.shiftKey)
                    {
                        buttonProp.isMinimal = !buttonProp.isMinimal;
                        UIbutton.Q<Label>("buttonLabel").text = buttonProp.isMinimal ? "" : buttonProp.Label;
                        UIbutton.tooltip = buttonProp.isMinimal ? buttonProp.Label : null;

                        var buttonShade = UIbutton.Q<VisualElement>("shade");
                        buttonShade.Q<Label>("buttonLabel").text = buttonProp.isMinimal ? "" : buttonProp.Label;

                        creatorButtons.SaveToJson();

                        evt.StopPropagation();
                    }
                    // color setting
                    else if (evt.altKey)
                    {
                        ShowBoxAtPos(UI.colorPopup, evt.position.x - 100);

                        ColorPopup.isForCreator = true;
                        ColorPopup.popupIsOpen = true;

                        ColorPopup.activeCreatorButton = buttonProp;
                        ColorPopup.activeVisualItem = UIbutton;

                        evt.StopPropagation();
                    }
                    else
                    {
                        depSelection?.Invoke(UIbutton);
                        AssetDatabase.Refresh();
                        EditorApplication.ExecuteMenuItem(buttonProp.menuEntry);

                        evt.StopPropagation();
                    }
                }
                else if (evt.button == 1)
                {
                    RemoveButton(buttonProp);

                    evt.StopPropagation();
                }
            });

        }


        // filling and removing
        public static void FillShortcutButtons()
        {
            UI.shortcutButtonArea.Clear();

            foreach (var button in navButtons.buttons)
            {
                UI.shortcutButtonArea.Add(button.UIbutton);
            }
        }
        public static void FillCreatorButtons()
        {
            UI.creatorButtonArea.Clear();

            foreach (var button in creatorButtons.buttons)
            {
                UI.creatorButtonArea.Add(button.UIbutton);
            }
        }

        public static void RemoveButton(ShortcutButton toRemove)
        {
            navButtons.RemoveButton(toRemove);

            FillShortcutButtons();
        }
        public static void RemoveButton(CreatorButton toRemove)
        {
            creatorButtons.RemoveButton(toRemove);

            FillCreatorButtons();
        }


        // shortcut bar dragging
        public static void SetupDragNDropForShortcutArea(VisualElement area, ShortcutButtonBundle navButtonsList)
        {
            area.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                PlaceholderNeedleAtPos(area, evt.localMousePosition.x);

                evt.StopPropagation();
            });

            area.RegisterCallback<DragPerformEvent>(evt =>
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));

                    Texture2D iconTexture = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;

                    var foundButton = navButtonsList.buttons.FirstOrDefault(b => b.buttonProp.guid == guid);
                    ShortcutButton buttonAtSamePath = foundButton?.buttonProp;

                    var button = new ShortcutButton(obj.name, guid);
                    if (buttonAtSamePath != null) button = new ShortcutButton(obj.name, guid, buttonAtSamePath.isMinimal, buttonAtSamePath.color);

                    if (placeHolderIndex != -1) navButtonsList.InsertAt(placeHolderIndex, button, UI.shortcutButtonAsset);

                    if (buttonAtSamePath != null) navButtonsList.RemoveButton(buttonAtSamePath);

                    UI.placeholderNeedle.RemoveFromHierarchy();
                    placeHolderIndex = -1;
                }

                FillShortcutButtons();

                evt.StopPropagation();
            });

            area.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    UI.placeholderNeedle.RemoveFromHierarchy();

                    placeHolderIndex = -1;
                }
            });
        }

        public static void SetupDragNDropForCreatorArea()
        {
            UI.creatorButtonArea.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                PlaceholderNeedleAtPos(UI.creatorButtonArea, evt.localMousePosition.x);

                evt.StopPropagation();
            });


            UI.creatorButtonArea.RegisterCallback<DragPerformEvent>(evt =>
            {
                DragAndDrop.AcceptDrag();
                UI.placeholderNeedle.RemoveFromHierarchy();

                Object[] objectReferences = DragAndDrop.objectReferences;
                if (objectReferences.Count() > 1)
                {
                    Debug.Log("[PieTabs]_Drag one item at a time");
                    return;
                }

                List<string> items = GetAssetCreateMenuEntries();

                var menu = new GenericMenu();
                foreach (var item in CleanEntries(items))
                {
                    var trimmedItem = item;
                    const string prefix = "Assets/Create/";

                    if (item.StartsWith(prefix))
                        trimmedItem = item.Substring(prefix.Length);

                    menu.AddItem(new GUIContent(trimmedItem), false, () =>
                    {
                        Object obj = DragAndDrop.objectReferences[0];
                        OnCreateMenuEntrySelected(obj, item);
                    });
                }
                menu.DropDown(new Rect(evt.mousePosition, Vector2.zero));

                evt.StopPropagation();
            });

            UI.creatorButtonArea.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    UI.placeholderNeedle.RemoveFromHierarchy();

                    placeHolderIndex = -1;
                }
            });

        }

    }

    static class UI
    {
        public static VisualElement projectBrowserUI;
        public static VisualElement mainUI;

        public static VisualElement placeholderNeedle;
        public static VisualElement copiedText;

        public static VisualElement colorPopup;


        public static VisualTreeAsset shortcutButtonAsset;
        public static VisualTreeAsset creatorButtonAsset;


        public static VisualElement shortcutButtonArea;
        public static VisualElement creatorButtonArea;
    }

    static class ColorPopup
    {
        public static bool isForCreator = false;
        public static bool popupIsOpen = false;

        public static ShortcutButton activeNavButton;
        public static CreatorButton activeCreatorButton;
        public static VisualElement activeVisualItem;
    }

}
#endif
