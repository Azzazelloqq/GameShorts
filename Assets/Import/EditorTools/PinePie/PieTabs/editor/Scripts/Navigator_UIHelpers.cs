// Copyright (c) 2025 PinePie. All rights reserved.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PinePie.PieTabs
{
    public static partial class Navigator
    {
        // UI Setup

        // split bars
        public static void SetupDragAreaStyling()
        {
            foreach (var view in new VisualElement[] { UI.shortcutButtonArea, UI.creatorButtonArea })
            {
                view.contentContainer.style.flexDirection = FlexDirection.RowReverse;
                view.contentContainer.style.justifyContent = Justify.FlexStart;
            }
        }

        public static void SetupSplitter()
        {
            VisualElement splitter = UI.mainUI.Q<VisualElement>("splitter");
            bool isDragging = false;
            int pointerId = -1;
            float distFromMouse = 0;



            splitter.RegisterCallback<PointerDownEvent>(evt =>
            {
                isDragging = true;
                pointerId = evt.pointerId;

                float mousePosFromRight = UI.mainUI.resolvedStyle.width - evt.position.x;
                distFromMouse = UI.creatorButtonArea.resolvedStyle.width - mousePosFromRight;

                splitter.CapturePointer(pointerId);

                evt.StopPropagation();
            });

            splitter.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isDragging || evt.pointerId != pointerId) return;

                float mousePos = UI.mainUI.resolvedStyle.width - evt.position.x;
                UI.creatorButtonArea.style.width = mousePos + distFromMouse;

                evt.StopPropagation();
            });

            splitter.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != pointerId) return;

                LastSplitterSpacing = UI.creatorButtonArea.resolvedStyle.width;
                isDragging = false;

                splitter.ReleasePointer(pointerId);
                evt.StopPropagation();
            });
        }

        public static void SetupSearchBarControls()
        {
            VisualElement splitter = UI.mainUI.Q<VisualElement>("splitter");
            VisualElement spacer = UI.mainUI.Q<VisualElement>("itemSpacer");
            VisualElement creatorButtonPopupAdder = UI.mainUI.Q<VisualElement>("createAssetButton");

            Button openSearchButton = UI.mainUI.Q<Button>("searchToggle");
            Button closeSearchButton = UI.mainUI.Q<Button>("closeSearchBar");

            ScrollView AssetCreatorButtonArea = UI.mainUI.Q<ScrollView>("CreationMenuDragArea");

            if (IsSearchBarOpen)
                OpenSearchBar(splitter, spacer, openSearchButton, closeSearchButton, AssetCreatorButtonArea);
            else
                CloseSearchBar(splitter, spacer, openSearchButton, closeSearchButton, AssetCreatorButtonArea);


            openSearchButton.clicked += () =>
            {
                OpenSearchBar(splitter, spacer, openSearchButton, closeSearchButton, AssetCreatorButtonArea);
            };

            closeSearchButton.clicked += () =>
            {
                CloseSearchBar(splitter, spacer, openSearchButton, closeSearchButton, AssetCreatorButtonArea);
            };
        }

        private static void CloseSearchBar(VisualElement splitter, VisualElement spacer, Button openSearchButton, Button closeSearchButton, ScrollView AssetCreatorButtonArea)
        {
            closeSearchButton.style.display = DisplayStyle.None;
            closeSearchButton.style.marginRight = 0f;

            spacer.style.display = DisplayStyle.Flex;
            if (IsTwoColumnMode())
            {
                splitter.style.display = DisplayStyle.Flex;
            }

            AssetCreatorButtonArea.style.width = LastSplitterSpacing;
            openSearchButton.style.display = DisplayStyle.Flex;

            IsSearchBarOpen = false;
        }

        private static void OpenSearchBar(VisualElement splitter, VisualElement spacer, Button openSearchButton, Button closeSearchButton, ScrollView AssetCreatorButtonArea)
        {
            closeSearchButton.style.display = DisplayStyle.Flex;
            closeSearchButton.style.marginRight = 465;

            splitter.style.display = DisplayStyle.None;
            spacer.style.display = DisplayStyle.None;

            AssetCreatorButtonArea.style.width = 0;
            openSearchButton.style.display = DisplayStyle.None;

            IsSearchBarOpen = true;
        }


        // address copy from bottom bar
        public static void SetupBottomBarMargin()
        {
            VisualElement bottomAddressBar = UI.mainUI.Q<VisualElement>("bottomAddressBar");

            bottomAddressBar.style.marginLeft = GetSideRectWidth(GetWin());
        }

        public static void RegisterAddressCopyCallbacks()
        {
            VisualElement bottomAddressBar = UI.mainUI.Q<VisualElement>("bottomAddressBar");
            bottomAddressBar.RegisterCallback<MouseDownEvent>((evt) =>
            {
                if (IsTwoColumnMode()) bottomAddressBar.style.marginLeft = GetSideRectWidth(GetWin());

                string copyingStr = "";

                if (evt.button == 0)
                {
                    copyingStr = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject))
                        ? AssetDatabase.GetAssetPath(Selection.activeObject)
                        : GetActiveFolderPath();
                }
                else if (evt.button == 1)
                {
                    string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    copyingStr = !string.IsNullOrEmpty(assetPath)
                        ? Path.GetFileName(assetPath)
                        : "";
                }

                EditorGUIUtility.systemCopyBuffer = copyingStr;

                ShowCopiedNotification(evt.mousePosition, UI.mainUI);
            });

            if (IsTwoColumnMode())
                Selection.selectionChanged += () =>
                {
                    SetupBottomBarMargin();
                };
        }

        public static void ShowCopiedNotification(Vector2 position, VisualElement root)
        {
            UI.copiedText.style.left = position.x - 20;

            UI.copiedText.style.display = DisplayStyle.Flex;

            root.schedule.Execute(() =>
            {
                UI.copiedText.style.display = DisplayStyle.None;
            }).ExecuteLater(1000);
        }


        // icon popup
        public static void ShowBoxAtPos(VisualElement box, float posX)
        {
            UI.mainUI.pickingMode = PickingMode.Position;

            float rightOffset = UI.mainUI.resolvedStyle.width - 200;

            box.style.left = Mathf.Clamp(posX, 0, rightOffset);

            box.style.display = DisplayStyle.Flex;
        }

        public static void CallbacksForPopupBoxes()
        {
            UI.mainUI.RegisterCallback<MouseDownEvent>((evt) =>
            {
                CloseAllPopups();
            });
        }

        private static void CloseAllPopups()
        {
            UI.colorPopup.style.display = DisplayStyle.None;

            ColorPopup.popupIsOpen = false;

            UI.mainUI.pickingMode = PickingMode.Ignore;
        }

        public static void CallbacksForColorPopup()
        {
            var colorButtons = UI.colorPopup.Query<Button>("icon").ToList();
            var removeColorBtn = UI.colorPopup.Q<Button>("removeColorBtn");

            foreach (Button clrBtn in colorButtons)
            {
                var buttonColor = clrBtn.resolvedStyle.backgroundColor;

                clrBtn.clicked += () =>
                {
                    if (!ColorPopup.popupIsOpen) return;

                    ApplyColorToVisualElement(ColorToHex(buttonColor));
                };
            }

            removeColorBtn.clicked += () =>
            {
                if (!ColorPopup.popupIsOpen) return;

                ApplyColorToVisualElement("#3E3E3E");
            };

            UI.colorPopup.RegisterCallback<MouseDownEvent>((evt) => evt.StopPropagation());
        }

        public static void ApplyColorToVisualElement(string hex)
        {
            if (ColorPopup.isForCreator)
            {
                ColorPopup.activeCreatorButton.color = hex;
                creatorButtons.SaveToJson();
            }
            else
            {
                ColorPopup.activeNavButton.color = hex;
                navButtons.SaveToJson();
            }

            ColorPopup.activeVisualItem.style.backgroundColor = HexToColor(hex);
            CloseAllPopups();
        }

    }
}

#endif