package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIMapBuild extends UIBase
   {
      
      private var _adLock:Boolean;
      
      public function UIMapBuild(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         param2.getChildByName("$ok").visible = false;
         (param2.getChildByName("loadword") as MovieClip).gotoAndStop(1);
         (param2.getChildByName("txtWords") as TextField).text = ConfigAndStory.MISSIONS.missionWords[ConfigAndStory.STORY];
         if(ConfigAndStory.STORY != 49)
         {
            (param2.getChildByName("txtTitle") as TextField).text = "BATTLE-" + (ConfigAndStory.STORY + 1);
         }
         else
         {
            (param2.getChildByName("txtTitle") as TextField).text = "CREDITS";
         }
         super(param1,param2,param3,param4,param5);
         if(!VeryOutShitABC.NOADS && !VeryOutShitABC.VGPLAY && (ConfigAndStory.STORY + 3) % 5 == 0)
         {
            this._adLock = true;
            Fly4UI.MAINFLY4.showAds(this.adFree);
         }
      }
      
      public function adFree() : void
      {
         this._adLock = false;
      }
      
      override public function afterFadeIn() : void
      {
         InitData.TARGETS.evts.core = InitData.CORE;
         InitData.CORE.pause = false;
         InitData.CORE.over = false;
         InitData.CORE.initLv();
         InitData.CORE.pause = true;
         _mov.getChildByName("$ok").visible = true;
         (_mov.getChildByName("loadword") as MovieClip).gotoAndStop(2);
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = null;
         if(!this._adLock)
         {
            _loc2_ = param1.name;
            if(_loc2_ == "ok")
            {
               InitData.CORE.pause = false;
               InitData.TARGETS.evts.gameui = new UIGame(_sp,Attach.attach("UI_Game") as MovieClip,"UI_Game",_ui);
               InitData.TARGETS.uiinit();
               InitData.CORE.playerCtrl();
               InitData.TARGETS.evts.talkSA();
               removeSelf();
            }
         }
      }
   }
}

