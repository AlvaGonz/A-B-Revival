package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.make.MakeUI;
   
   public class UIAllWin extends UIBase
   {
      
      public var lastMoney:int;
      
      public var repaired:Boolean = false;
      
      public function UIAllWin(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         var _loc7_:SoundUnit2 = null;
         var _loc6_:int = int(Math.random() * ConfigAndStory.MISSIONS.tips.length);
         (param2.getChildByName("tip") as TextField).text = "Tip " + (_loc6_ + 1) + " : " + ConfigAndStory.MISSIONS.tips[_loc6_];
         InitData.SOUNDC2.allEnd(true);
         _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[10],"M_Win2");
         InitData.SOUNDC2.addSound(_loc7_,int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
         this.lastMoney = ConfigAndStory.MONEY;
         if(ConfigAndStory.ALLPART.repairPart.length == 0)
         {
            param2.getChildByName("$repair").visible = false;
            this.repaired = true;
         }
         super(param1,param2,param3,param4,param5);
         ConfigAndStory.MONEY += ConfigAndStory.ALLPART.levelMoney;
         MochiEvents.trackEvent("all_win",ConfigAndStory.STORY);
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "see")
         {
            if(this.repaired)
            {
               this.toNext();
            }
            else
            {
               new UIReally(this.toNext,null,null,"Continue?","Wanna skip repairs?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         else if(_loc2_ == "restart")
         {
            new UIReally(this.retry,null,null,"Retry?","Wanna retry this battle?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(_loc2_ == "repair")
         {
            this.repaired = true;
            new UIRepair(ConfigAndStory.ALLPART,_sp,Attach.attach("UI_Repair") as MovieClip,"UI_Repair",_ui,this);
         }
         else if(_loc2_ == "link")
         {
            InitData.vargames();
         }
      }
      
      private function toNext() : void
      {
         InitData.CORE.repair.shopNewFalse();
         InitData.CORE.repair.removeParts();
         ConfigAndStory.STORY = 49;
         allRemoved = function():void
         {
            nextGame();
         };
         removeSelf();
      }
      
      public function nextGame() : void
      {
         MakeUI.makeChoose(this,_sp,_ui);
         ConfigAndStory.saveData();
         ConfigAndStory.save();
      }
      
      public function retry() : void
      {
         InitData.CORE.repair.deadFalse();
         ConfigAndStory.MONEY = this.lastMoney;
         MakeUI.makeChoose(this,_sp,_ui);
         ConfigAndStory.saveData();
         ConfigAndStory.save();
         removeSelf();
      }
   }
}

