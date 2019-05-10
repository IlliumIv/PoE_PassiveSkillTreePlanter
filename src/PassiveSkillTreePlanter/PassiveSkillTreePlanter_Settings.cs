using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using SharpDX;
using ImGuiVector2 = System.Numerics.Vector2;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanterSettings : SettingsBase
    {
        public PassiveSkillTreePlanterSettings()
        {
            Vector2 centerPos = BasePlugin.API.GameController.Window.GetWindowRectangle().Center;
            Enable = false;
            BorderWidth = new RangeNode<int>(1, 1, 5);
            LineWidth = new RangeNode<int>(3, 0, 5);
            LineColor = new ColorNode(new Color(0, 255, 0, 50));
            SelectedURLFile = string.Empty;
        }
        public RangeNode<int> offsetX { get; set; } = new RangeNode<int>(12465, 11000, 14000);
        public RangeNode<int> offsetY { get; set; } = new RangeNode<int>(11582, 11000, 13000);

        public RangeNode<int> BorderWidth { get; set; }

        public RangeNode<int> LineWidth { get; set; }

        public ColorNode LineColor { get; set; }

        public string SelectedURLFile { get; set; }
        public string SelectedURL { get; set; } = "";
        public string SelectedTreeName { get; set; } = "";


        public RangeNode<int> PickedBorderWidth { get; set; } = new RangeNode<int>(1, 1, 5);
        public RangeNode<int> UnpickedBorderWidth { get; set; } = new RangeNode<int>(3, 1, 5);
        public RangeNode<int> WrongPickedBorderWidth { get; set; } = new RangeNode<int>(3, 1, 5);

        public ColorNode PickedBorderColor { get; set; } = new ColorNode(Color.Gray);
        public ColorNode UnpickedBorderColor { get; set; } = new ColorNode(Color.Green);
        public ColorNode WrongPickedBorderColor { get; set; } = new ColorNode(Color.Red);

        public TreeConfig.SkillTreeData SelectedBuild { get; set; } = new TreeConfig.SkillTreeData();
        public TreeConfig.SkillTreeData SelectedBuildCreating { get; set; } = new TreeConfig.SkillTreeData();

        public ToggleNode EnableEzTreeChanger { get; set; } = true;
    }
}