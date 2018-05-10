﻿using ImGuiNET;
using PassiveSkillTreePlanter.SkillTreeJson;
using PassiveSkillTreePlanter.UrlDecoders;
using PoeHUD.Framework;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vector2 = System.Numerics.Vector2;

namespace PassiveSkillTreePlanter
{
    public class PassiveSkillTreePlanter : BaseSettingsPlugin<PassiveSkillTreePlanterSettings>
    {
        private const string SkillTreeDataFile = "SkillTreeData.dat";
        private const string SkillTreeDir = "Builds";
        public static int selected;
        private PoESkillTreeJsonDecoder _skillTreeeData = new PoESkillTreeJsonDecoder();


        private bool _bUiRootInitialized;
        private List<SkillNode> _drawNodes = new List<SkillNode>();

        private Element _uiSkillTreeBase;
        private List<ushort> _urlNodes = new List<ushort>(); //List of nodes decoded from URL

        public string AddNewBuildFile = "";
        public string AddNewBuildUrl = "";
        public string AddNewBuildThreadUrl = "";
        public string CurrentlySelectedBuildFile { get; set; }
        public string CurrentlySelectedBuildFileEdit { get; set; }
        public string CurrentlySelectedBuildUrl { get; set; }
        public string CurrentlySelectedBuildForumThread { get; set; }

        private string SkillTreeUrlFilesDir => LocalPluginDirectory + @"\" + SkillTreeDir;
        private List<string> BuildFiles { get; set; }
        public IntPtr TextEditCallback { get; set; }

        public override void Initialise()
        {
            _skillTreeeData = new PoESkillTreeJsonDecoder();
            _urlNodes.Clear();
            _drawNodes.Clear();

            if (!Directory.Exists(SkillTreeUrlFilesDir))
            {
                Directory.CreateDirectory(SkillTreeUrlFilesDir);
                LogMessage("PassiveSkillTree: Write your skill tree url to txt file and place it to Builds folder.", 10);
                return;
            }

            LoadBuildFiles();

            //Read url
            ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
        }

        public override void Render()
        {
            base.Render();
            ExtRender();
        }

        private void LoadBuildFiles()
        {
            //Update url list variants
            var dirInfo = new DirectoryInfo(SkillTreeUrlFilesDir);
            BuildFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
        }

        private static string CleanFileName(string fileName) { return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty)); }

        public void RenameFile(string fileName, string oldFileName)
        {
            fileName = CleanFileName(fileName);
            var newFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            var oldFilePath = Path.Combine(SkillTreeUrlFilesDir, oldFileName + ".txt");
            if (File.Exists(newFilePath))
            {
                LogError("PassiveSkillTreePlanter: File already Exists!", 10);
                return;
            }

            //Now Rename the File
            File.Move(oldFilePath, newFilePath);
            LoadBuildFiles();
            Settings.SelectedURLFile = fileName;
            ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
        }

        public string RemoveAccName(string url)
        {
            // Aim is to remove the string content but keep the info inside the text file incase user wants to revisit that account/char in the future
            if (url.Contains("?accountName"))
                url = url.Split(new[]
                {
                        "?accountName"
                }, StringSplitOptions.None)[0];
            // If string contains characterName but no 
            if (url.Contains("?characterName"))
                url = url.Split(new[]
                {
                        "?characterName"
                }, StringSplitOptions.None)[0];
            return url;
        }

        public void ReplaceUrlContents(string fileName, string newContents)
        {
            fileName = CleanFileName(fileName);
            var filePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(filePath))
            {
                LogError("PassiveSkillTreePlanter: File doesnt exist", 10);
                return;
            }

            if (!IsBase64String(RemoveAccName(newContents)))
            {
                LogError("PassiveSkillTreePlanter: Invalid URL or you are trying to add something that is not a pathofexile.com build URL", 10);
                return;
            }

            //Now Rename the File
            File.WriteAllText(filePath, newContents);
            ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
        }

        public void ReplaceThreadUrlContents(string fileName, string newContents)
        {
            var skillTreeUrlFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(skillTreeUrlFilePath))
                return;
            var strings = File.ReadAllLines(skillTreeUrlFilePath);
            var newString = new List<string>
            {
                    strings[0],
                    newContents
            };
            File.WriteAllLines(skillTreeUrlFilePath, newString);
        }

        public void AddNewBuild(string buildName, string buildUrl, string buildThreadUrl)
        {
            var newFilePath = Path.Combine(SkillTreeUrlFilesDir, buildName + ".txt");
            if (File.Exists(newFilePath))
            {
                LogError("PassiveSkillTreePlanter.Add: File already Exists!", 10);
                return;
            }

            if (!IsBase64String(buildUrl))
            {
                LogError("PassiveSkillTreePlanter.Add: Invalid URL or you are trying to add something that is not a pathofexile.com build URL OR you need to remove the game version (3.x.x)", 10);
                return;
            }

            var newString = new List<string>();
            newString.Add(buildUrl);
            if (!string.IsNullOrEmpty(buildThreadUrl))
            {
                newString.Add(buildThreadUrl);
            }
            File.WriteAllLines(newFilePath, newString);
            LoadBuildFiles();
        }

        public static bool IsBase64String(string url)
        {
            try
            {
                url = url.Split('/').LastOrDefault()?.Replace("-", "+").Replace("_", "/");
                // If no exception is caught, then it is possibly a base64 encoded string
                var data = Convert.FromBase64String(url);
                // The part that checks if the string was properly padded to the
                return url.Replace(" ", "").Length % 4 == 0;
            }
            catch
            {
                // If exception is caught, then it is not a base64 encoded string
                return false;
            }
        }
        
        public override void DrawSettingsMenu()
        {
            string[] settingName =
            {
                    "Build Selection",
                    "Build Edit",
                    "Add Build",
                    "Colors",
                    "Sliders"
            };
            ImGuiNative.igGetContentRegionAvail(out var newcontentRegionArea);
            if (ImGui.BeginChild("LeftSettings", new Vector2(150, newcontentRegionArea.Y), false, WindowFlags.Default))
                for (var i = 0; i < settingName.Length; i++)
                    if (ImGui.Selectable(settingName[i], selected == i))
                        selected = i;
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.PushStyleVar(StyleVar.ChildRounding, 5.0f);
            ImGuiNative.igGetContentRegionAvail(out newcontentRegionArea);
            if (ImGui.BeginChild("RightSettings", new Vector2(newcontentRegionArea.X, newcontentRegionArea.Y), true, WindowFlags.Default))
                switch (settingName[selected])
                {
                    case "Build Selection":
                        if (ImGui.Button("Open Build Folder")) Process.Start(SkillTreeUrlFilesDir);
                        ImGui.SameLine();
                        if (ImGui.Button("Reload List")) LoadBuildFiles();
                        ImGui.SameLine();
                        if (ImGui.Button("Open Forum Thread")) ReadHtmlLineFromFile(Settings.SelectedURLFile);
                        Settings.SelectedURLFile = ImGuiExtension.ComboBox("Build Files", Settings.SelectedURLFile, BuildFiles, out var tempBool, ComboFlags.HeightLarge);
                        if (tempBool) ReadUrlFromSelectedUrl(Settings.SelectedURLFile);
                        List<string> Notes = ReadNotesFromSelectedBuild(Settings.SelectedURLFile);
                        if (Notes.Count > 0)
                        {
                            ImGui.Spacing();
                            ImGui.Spacing();
                            ImGui.BulletText("Notes");
                            ImGui.Spacing();
                            foreach (string line in Notes) ImGui.Text(line);
                        }
                        break;
                    case "Build Edit":
                        if (!string.IsNullOrEmpty(CurrentlySelectedBuildFile))
                        {
                            CurrentlySelectedBuildFileEdit = ImGuiExtension.InputText("##RenameLabel", CurrentlySelectedBuildFileEdit, 1024, InputTextFlags.EnterReturnsTrue);
                            ImGui.SameLine();
                            if (ImGui.Button("Rename Build")) RenameFile(CurrentlySelectedBuildFileEdit, Settings.SelectedURLFile);

                            CurrentlySelectedBuildUrl = ImGuiExtension.InputText("##ChangeURL", CurrentlySelectedBuildUrl, 1024, InputTextFlags.EnterReturnsTrue | InputTextFlags.AutoSelectAll);
                            ImGui.SameLine();
                            if (ImGui.Button("Change URL")) ReplaceUrlContents(Settings.SelectedURLFile, CurrentlySelectedBuildUrl);

                            CurrentlySelectedBuildForumThread = ImGuiExtension.InputText("##ChangeThreadURL", CurrentlySelectedBuildForumThread, 1024, InputTextFlags.EnterReturnsTrue | InputTextFlags.AutoSelectAll);
                            ImGui.SameLine();
                            if (ImGui.Button("Change Thread URL")) ReplaceThreadUrlContents(Settings.SelectedURLFile, CurrentlySelectedBuildForumThread);
                        }
                        else
                            ImGui.Text("No Build Selected");

                        break;
                    case "Add Build":
                        AddNewBuildFile = ImGuiExtension.InputText("Build Name", AddNewBuildFile, 1024, InputTextFlags.EnterReturnsTrue);
                        AddNewBuildUrl = ImGuiExtension.InputText("Build URL", AddNewBuildUrl, 1024, InputTextFlags.EnterReturnsTrue | InputTextFlags.AutoSelectAll);
                        AddNewBuildThreadUrl = ImGuiExtension.InputText("Build Thread URL", AddNewBuildThreadUrl, 1024, InputTextFlags.EnterReturnsTrue | InputTextFlags.AutoSelectAll);
                        if (ImGui.Button("Create Build"))
                        {
                            AddNewBuild(AddNewBuildFile, AddNewBuildUrl, AddNewBuildThreadUrl);
                            AddNewBuildFile = "";
                            AddNewBuildUrl = "";
                            AddNewBuildThreadUrl = "";
                        }

                        break;
                    case "Colors":
                        Settings.BorderColor.Value = ImGuiExtension.ColorPicker("Border Color", Settings.BorderColor);
                        Settings.LineColor.Value = ImGuiExtension.ColorPicker("Line Color", Settings.LineColor);
                        break;
                    case "Sliders":
                        Settings.BorderWidth.Value = ImGuiExtension.IntSlider("Border Width", Settings.BorderWidth);
                        Settings.LineWidth.Value = ImGuiExtension.IntSlider("Line Width", Settings.LineWidth);
                        break;
                }
            ImGui.PopStyleVar();
            ImGui.EndChild();
        }

        private List<string> ReadNotesFromSelectedBuild(string fileName)
        {
            string BuildFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            List<string> tempList = new List<string>();
            if (!File.Exists(BuildFilePath))
            {
                return tempList;
            }

            string[] strings = File.ReadAllLines(BuildFilePath);
            if (strings.Length <= 2) return tempList;
            for (int i = 2; i < strings.Length; i++)
            {
                string line = strings[i];
                string replace = line.Replace('–', '-');
                tempList.Add(replace);
            }

            return tempList;
        }

        private void ReadHtmlLineFromFile(string fileName)
        {
            var skillTreeUrlFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(skillTreeUrlFilePath))
            {
                LogMessage("PassiveSkillTree: Select build url from list in options.", 10);
                return;
            }

            var strings = File.ReadAllLines(skillTreeUrlFilePath);
            if (strings.Length == 1 || strings[1] == string.Empty)
            {
                LogMessage("PassiveSkillTree: This build has no saved Forum link.", 10);
                return;
            }

            if (strings[1] != null && strings[1] != string.Empty)
            {
                System.Diagnostics.Process.Start(strings[1]);
            }
        }

        private string GetHtmlLineFromFile(string fileName)
        {
            var skillTreeUrlFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(skillTreeUrlFilePath))
            {
                return string.Empty;
            }

            var strings = File.ReadAllLines(skillTreeUrlFilePath);
            if (strings.Length == 1)
            {
                return string.Empty;
            }

            if (strings[1] != null && strings[1] != string.Empty)
            {
                return strings[1];
            }

            return string.Empty;
        }

        private void ReadUrlFromSelectedUrl(string fileName)
        {
            var skillTreeUrlFilePath = Path.Combine(SkillTreeUrlFilesDir, fileName + ".txt");
            if (!File.Exists(skillTreeUrlFilePath))
            {
                LogMessage("PassiveSkillTree: Select build url from list in options.", 10);
                Settings.SelectedURLFile = string.Empty;
                return;
            }

            var skillTreeUrl = File.ReadLines(skillTreeUrlFilePath).First();

            // replaces the game tree version "x.x.x/"
            var rgx = new Regex("^https:\\/\\/www.pathofexile.com\\/fullscreen-passive-skill-tree\\/(([0-9]\\W){3})", RegexOptions.IgnoreCase);
            var match = rgx.Match(skillTreeUrl);
            if (match.Success)
                skillTreeUrl = skillTreeUrl.Replace(match.Groups[1].Value, "");
            rgx = new Regex("^https:\\/\\/www.pathofexile.com\\/passive-skill-tree\\/(([0-9]\\W){3})", RegexOptions.IgnoreCase);
            match = rgx.Match(skillTreeUrl);
            if (match.Success)
                skillTreeUrl = skillTreeUrl.Replace(match.Groups[1].Value, "");

            // remove ?accountName and such off the end of the string
            skillTreeUrl = RemoveAccName(skillTreeUrl);
            if (!DecodeUrl(skillTreeUrl))
            {
                LogMessage("PassiveSkillTree: Can't decode url from file: " + skillTreeUrlFilePath, 10);
                return;
            }

            CurrentlySelectedBuildFile = fileName;
            CurrentlySelectedBuildFileEdit = fileName;
            CurrentlySelectedBuildUrl = File.ReadAllLines(skillTreeUrlFilePath).First();
            CurrentlySelectedBuildForumThread = GetHtmlLineFromFile(fileName);
            ProcessNodes();
        }

        private void ProcessNodes()
        {
            _drawNodes = new List<SkillNode>();

            //Read data
            var skillTreeDataPath = LocalPluginDirectory + @"\" + SkillTreeDataFile;
            if (!File.Exists(skillTreeDataPath))
            {
                LogMessage("PassiveSkillTree: Can't find file " + SkillTreeDataFile + " with skill tree data.", 10);
                return;
            }

            var skillTreeJson = File.ReadAllText(skillTreeDataPath);
            _skillTreeeData.Decode(skillTreeJson);
            foreach (var urlNodeId in _urlNodes)
            {
                if (!_skillTreeeData.Skillnodes.ContainsKey(urlNodeId))
                {
                    LogError("PassiveSkillTree: Can't find passive skill tree node with id: " + urlNodeId, 5);
                    continue;
                }

                var node = _skillTreeeData.Skillnodes[urlNodeId];
                node.Init();
                _drawNodes.Add(node);
                var dontDrawLinesTwice = new List<ushort>();
                foreach (var lNodeId in node.linkedNodes)
                {
                    if (!_urlNodes.Contains(lNodeId)) continue;
                    if (dontDrawLinesTwice.Contains(lNodeId)) continue;
                    if (!_skillTreeeData.Skillnodes.ContainsKey(lNodeId))
                    {
                        LogError("PassiveSkillTree: Can't find passive skill tree node with id: " + lNodeId + " to draw the link", 5);
                        continue;
                    }

                    var lNode = _skillTreeeData.Skillnodes[lNodeId];
                    node.DrawNodeLinks.Add(lNode.Position);
                }

                dontDrawLinesTwice.Add(urlNodeId);
            }
        }


        private bool DecodeUrl(string url)
        {
            if (PoePlannerUrlDecoder.UrlMatch(url))
            {
                _urlNodes = PoePlannerUrlDecoder.Decode(url);
                return true;
            }

            if (PathOfExileUrlDecoder.UrlMatch(url))
            {
                _urlNodes = PathOfExileUrlDecoder.Decode(url);
                return true;
            }

            return false;
        }

        private async void DownloadTree()
        {
            var skillTreeDataPath = LocalPluginDirectory + @"\" + SkillTreeDataFile;
            await PassiveSkillTreeJson_Downloader.DownloadSkillTreeToFileAsync(skillTreeDataPath);
            LogMessage("Skill tree updated!", 3);
        }

        private void ExtRender()
        {
            if (_drawNodes.Count == 0) return;
            if (!GameController.InGame || !WinApi.IsForegroundWindow(GameController.Window.Process.MainWindowHandle)) return;
            var treePanel = GameController.Game.IngameState.IngameUi.TreePanel;
            if (!treePanel.IsVisible)
            {
                _bUiRootInitialized = false;
                _uiSkillTreeBase = null;
                return;
            }

            if (!_bUiRootInitialized)
            {
                _bUiRootInitialized = true;
                //I still can't find offset for Skill Tree root, so I made it by checking
                foreach (var child in treePanel.Children) //Only 8 childs for check
                    if (child.Width > 20000 && child.Width < 30000)
                    {
                        _uiSkillTreeBase = child;
                        break;
                    }
            }

            if (_uiSkillTreeBase == null)
            {
                LogError("Can't find UiSkillTreeBase root!", 0);
                return;
            }

            var scale = _uiSkillTreeBase.Scale;

            //Hand-picked values
            var offsetX = 12465;
            var offsetY = 11582;
            foreach (var node in _drawNodes)
            {
                var drawSize = node.DrawSize * scale;
                var posX = (_uiSkillTreeBase.X + node.DrawPosition.X + offsetX) * scale;
                var posY = (_uiSkillTreeBase.Y + node.DrawPosition.Y + offsetY) * scale;
                Graphics.DrawFrame(new RectangleF(posX - drawSize / 2, posY - drawSize / 2, drawSize, drawSize), Settings.BorderWidth, Settings.BorderColor);
                foreach (var link in node.DrawNodeLinks)
                {
                    var linkDrawPosX = (_uiSkillTreeBase.X + link.X + offsetX) * scale;
                    var linkDrawPosY = (_uiSkillTreeBase.Y + link.Y + offsetY) * scale;
                    if (Settings.LineWidth > 0) Graphics.DrawLine(new SharpDX.Vector2(posX, posY), new SharpDX.Vector2(linkDrawPosX, linkDrawPosY), Settings.LineWidth, Settings.LineColor);
                }
            }
        }
    }
}