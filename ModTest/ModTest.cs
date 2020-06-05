using System;
using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;
using TaleWorlds.GauntletUI.Data;

namespace ModTest
{
    public class SubModule : MBSubModuleBase
    {
        private bool _isLoaded;

        private bool _keyPressed;

        [DllImport("Rgl.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?toggle_imgui_console_visibility@rglCommand_line_manager@@QEAAXXZ")]
        public static extern void toggle_imgui_console_visibility(UIntPtr x);

        protected override void OnSubModuleLoad(){
            base.OnSubModuleLoad();
            /*在主菜单中添加一个选项，命名为Message，点击后在左下角显示信息“Hello World!”*/
            TaleWorlds.MountAndBlade.Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Message",
                new TextObject("Message", null),
                9990,
                () => { InformationManager.DisplayMessage(new InformationMessage("Hello World!")); },
                false));
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            /*在主菜单加载前，有入场动画，在动画的左下角，显示自定义信息“Loaded DeveloperConsole. Press CTRL and ~ to enable it.”*/
            if (!this._isLoaded)
            {
                InformationManager.DisplayMessage(new InformationMessage("Loaded DeveloperConsole. Press CTRL and ~ to enable it.", Color.FromUint(4282569842U)));
                this._isLoaded = true;
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (!this._keyPressed && Input.DebugInput.IsControlDown() && Input.DebugInput.IsKeyDown(InputKey.Tilde))
            {
                SubModule.toggle_imgui_console_visibility(new UIntPtr(1U));
                this._keyPressed = true;
            }
            else if (Input.DebugInput.IsKeyReleased(InputKey.Tilde))
            {
                this._keyPressed = false;
            }
            if (Commands.IsUIDebugMode)
            {
                UIResourceManager.UIResourceDepot.CheckForChanges();
            }
        }
    }

	public static class Commands
	{
		public static bool IsUIDebugMode;

		[CommandLineFunctionality.CommandLineArgumentFunction("unlock_all_parts", "crafting")]
		public static string UnlockAllParts(List<string> strings)
		{
			if (Campaign.Current == null)
			{
				return "Campaign was not started.";
			}
			CraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>();
			MethodInfo method = campaignBehavior.GetType().GetMethod("OpenPart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				return "Failed to locate method.";
			}
			for (int i = 0; i < CraftingPiece.All.Count; i++)
			{
				method.Invoke(campaignBehavior, new object[]
				{
					CraftingPiece.All[i]
				});
			}
			return "Unlocked all crafting pieces.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("list_active_quests", "campaign")]
		public static string ListActiveQuests(List<string> strings)
		{
			if (Campaign.Current == null)
			{
				return "Campaign was not started.";
			}
			string text = string.Empty;
			for (int i = 0; i < Campaign.Current.QuestManager.Quests.Count; i++)
			{
				QuestBase questBase = Campaign.Current.QuestManager.Quests[i];
				text += string.Format("{0}\n", questBase.Title);
			}
			return text;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("cancel_active_quest", "campaign")]
		public static string CancelQuest(List<string> strings)
		{
			if (Campaign.Current == null)
			{
				return "Campaign was not started.";
			}
			if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
			{
				return "Format is \"campaign.cancel_active_quest [QuestName]\".";
			}
			string questName = string.Join(" ", strings.ToArray()).Trim(new char[]
			{
				'"'
			});
			QuestBase questBase = Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase q) => q.Title.ToString().ToLower().Contains(questName.ToLower()));
			if (questBase == null)
			{
				return "Failed to find active quest with name \"" + questName + "\"";
			}
			questBase.CompleteQuestWithCancel(null);
			return string.Format("{0} has been cancelled.", questBase.Title);
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("complete_active_quest", "campaign")]
		public static string CompleteQuest(List<string> strings)
		{
			if (Campaign.Current == null)
			{
				return "Campaign was not started.";
			}
			if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
			{
				return "Format is \"campaign.complete_active_quest [QuestName]\".";
			}
			string questName = string.Join(" ", strings.ToArray()).Trim(new char[]
			{
				'"'
			});
			QuestBase questBase = Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase q) => q.Title.ToString().ToLower().Contains(questName.ToLower()));
			if (questBase == null)
			{
				return "Failed to find active quest with name \"" + questName + "\"";
			}
			MethodInfo method = questBase.GetType().GetMethod("CompleteQuestWithSuccess", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				return "Failed to locate method.";
			}
			method.Invoke(questBase, null);
			return string.Format("{0} has been completed.", questBase.Title);
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("spawn_prop", "mission")]
		public static string SpawnProp(List<string> strings)
		{
			if (Campaign.Current == null)
			{
				return "Campaign was not started.";
			}
			if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
			{
				return "Format is \"mission.spawn_prop [PropId]\".";
			}
			if (Mission.Current == null)
			{
				return "You are not in a mission.";
			}
			GameEntity gameEntity;
			if (Agent.Main == null)
			{
				MissionScreen missionScreen = ScreenManager.TopScreen as MissionScreen;
				if (missionScreen != null && missionScreen.CombatCamera != null)
				{
					gameEntity = GameEntity.Instantiate(Mission.Current.Scene, strings[0], missionScreen.CombatCamera.Frame);
					goto IL_99;
				}
			}
			gameEntity = GameEntity.Instantiate(Mission.Current.Scene, strings[0], Agent.Main.Frame);
		IL_99:
			if (gameEntity == null)
			{
				return "Failed to spawn prop with id \"" + strings[0] + "\"";
			}
			return "Spawned " + gameEntity.Name + ".";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("set_main_agent_health", "mission")]
		public static string SetMainAgentHealth(List<string> strings)
		{
			if (Campaign.Current == null)
			{
				return "Campaign was not started.";
			}
			int num;
			if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings) || !int.TryParse(strings[0], out num))
			{
				return "Format is \"mission.set_main_agent_health [Integer]\".";
			}
			if (Mission.Current == null)
			{
				return "You are not in a mission.";
			}
			if (Agent.Main == null)
			{
				return "No main agent found.";
			}
			if (num < 0)
			{
				num = 0;
			}
			if (num > 10000000)
			{
				num = 10000000;
			}
			if ((float)num > Agent.Main.HealthLimit)
			{
				Agent.Main.HealthLimit = (float)num;
			}
			Agent.Main.Health = (float)num;
			return string.Format("Set main agent health to {0}", num);
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("toggle_debug_mode", "ui")]
		public static string ToggleUIDebugMode(List<string> strings)
		{
			if (Commands.IsUIDebugMode)
			{
				UIResourceManager.UIResourceDepot.StopWatchingChangesInDepot();
			}
			else
			{
				UIResourceManager.UIResourceDepot.StartWatchingChangesInDepot();
			}
			Commands.IsUIDebugMode = !Commands.IsUIDebugMode;
			return "UI Debug Mode " + (Commands.IsUIDebugMode ? "Enabled" : "Disabled");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("open_screen", "ui")]
		public static string OpenScreen(List<string> strings)
		{
			if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
			{
				return "Format is \"ui.open_screen [TypeName]\".";
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Func<Type, bool> <> 9__0;
			for (int i = 0; i < assemblies.Length; i++)
			{
				IEnumerable<Type> types = assemblies[i].GetTypes();
				Func<Type, bool> predicate;
				if ((predicate = <> 9__0) == null)
				{
					predicate = (<> 9__0 = ((Type t) => t.Name == strings[0]));
				}
				Type type = types.FirstOrDefault(predicate);
				if (type != null && type.BaseType == typeof(ScreenBase))
				{
					MethodInfo methodInfo = typeof(ViewCreatorManager).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "CreateScreenView" && m.GetParameters().Length == 0);
					ScreenBase screenBase = ((methodInfo != null) ? methodInfo.MakeGenericMethod(new Type[]
					{
						type
					}).Invoke(null, null) : null) as ScreenBase;
					if (screenBase != null)
					{
						ScreenManager.PushScreen(screenBase);
						return "Pushed Screen " + type.Name + ".";
					}
				}
			}
			return "Could not find screen with name " + strings[0] + ".";
		}
	}

	public class MyExampleScreen : ScreenBase
	{
		private GauntletLayer _gauntletLayer;

		private GauntletMovie _movie;
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._gauntletLayer = new GauntletLayer(100, "GauntletLayer")
			{
				IsFocusLayer = true
			};
			base.AddLayer(this._gauntletLayer);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._movie = this._gauntletLayer.LoadMovie("ExScreen", null);
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			ScreenManager.TrySetFocus(this._gauntletLayer);
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
		}

		protected override void OnFinalize()
		{
			base.OnFinalize();
			base.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer = null;
		}
	}
}
