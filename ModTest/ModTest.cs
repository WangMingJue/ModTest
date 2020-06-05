using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ModTest
{
    public class Main:MBSubModuleBase
    {
        protected override void OnSubModuleLoad(){
            base.OnSubModuleLoad();
            /*在主菜单中添加一个选项，命名为Message，点击后在左下角显示信息“Hello World!”*/
            Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Message",
                new TextObject("Message", null),
                9990,
                () => { InformationManager.DisplayMessage(new InformationMessage("Hello World!")); },
                false));
        }
    }
}
