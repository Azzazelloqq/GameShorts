using System;

namespace Code.Generated.Addressables
{
    public class GameAngryHumans
    {
        public string Target = "Target";
        public string Block = "Block";
        public string MainGame = "MainGame";

        public readonly ContentSubGroup Content = new ContentSubGroup();
        public readonly AnimationControllersSubGroup AnimationControllers = new AnimationControllersSubGroup();
        public readonly AnimationsSubGroup Animations = new AnimationsSubGroup();
        public readonly MaterialsSubGroup Materials = new MaterialsSubGroup();
        public readonly AssetsSubGroup Assets = new AssetsSubGroup();

        public class ContentSubGroup
        {
            public string Human10 = "Content/Human10";
            public string HumanHelmet1 = "Content/HumanHelmet1";
            public string Human1 = "Content/Human1";
            public string Human6 = "Content/Human6";
            public string Human3 = "Content/Human3";
            public string HumanHelmet3 = "Content/HumanHelmet3";
            public string Human5 = "Content/Human5";
            public string Human8 = "Content/Human8";
            public string HumanHelmet4 = "Content/HumanHelmet4";
            public string HumanHelmet2 = "Content/HumanHelmet2";
            public string Human9 = "Content/Human9";
            public string Human4 = "Content/Human4";
            public string Human7 = "Content/Human7";
            public string Human11 = "Content/Human11";
            public string Human2 = "Content/Human2";
            public string Human12 = "Content/Human12";
        }

        public class AnimationControllersSubGroup
        {
            public string Idle = "AnimationControllers/Idle";
        }

        public class AnimationsSubGroup
        {
            public string Idle = "Animations/Idle";
        }

        public class MaterialsSubGroup
        {
            public string TrajectoryMaterialDefault = "Materials/TrajectoryMaterialDefault";
        }

        public class AssetsSubGroup
        {
            public class GamesSubGroup
            {
                public class AngryHumansSubGroup
                {
                    public class ContentSubGroup
                    {
                        public class TargetsSubGroup
                        {
                            public class LevelStructuresSubGroup
                            {
                                public string FirstLevelPrefab = "Assets/Games/AngryHumans/Content/Targets/LevelStructures/FirstLevel.prefab";
                            }
                        }
                    }
                }
            }
        }
    }
}
