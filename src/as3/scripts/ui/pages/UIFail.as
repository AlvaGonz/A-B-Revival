package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.make.MakeUI;
   
   public class UIFail extends UIBase
   {
      
      public function UIFail(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         var _loc6_:int = int(Math.random() * ConfigAndStory.MISSIONS.tips.length);
         (param2.getChildByName("tip") as TextField).text = "Tip " + (_loc6_ + 1) + " : " + ConfigAndStory.MISSIONS.tips[_loc6_];
         super(param1,param2,param3,param4,param5);
         MochiEvents.trackEvent("fail",ConfigAndStory.STORY);
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "retry")
         {
            InitData.CORE.repair.deadFalse();
            if(ConfigAndStory.STORY == 8)
            {
               --ConfigAndStory.STORY;
            }
            MakeUI.makeChoose(this,_sp,_ui);
            ConfigAndStory.saveData();
            ConfigAndStory.save();
            removeSelf();
         }
         else if(_loc2_ == "main")
         {
            new UIReally(this.mainYes,null,null,"To Main Menu?","\nWanna Quit?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(_loc2_ == "link")
         {
            InitData.vargames();
         }
      }
      
      public function mainYes() : void
      {
         InitData.CORE.repair.deadFalse();
         if(ConfigAndStory.STORY == 8)
         {
            --ConfigAndStory.STORY;
         }
         new UIMain(_sp,Attach.attach("UI_Main") as MovieClip,"UI_Main",_ui);
         ConfigAndStory.saveData();
         ConfigAndStory.save();
         removeSelf();
      }
   }
}

