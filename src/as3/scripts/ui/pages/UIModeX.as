package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.text.TextField;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.make.MakeUI;
   
   public class UIModeX extends UIBase
   {
      
      public var lastMoney:int = ConfigAndStory.MONEY;
      
      public var tempMoney:Number = 0;
      
      public var repaired:Boolean = false;
      
      public function UIModeX(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         var _loc7_:SoundUnit2 = null;
         var _loc6_:int = int(Math.random() * ConfigAndStory.MISSIONS.tips.length);
         (param2.getChildByName("tip") as TextField).text = "Tip " + (_loc6_ + 1) + " : " + ConfigAndStory.MISSIONS.tips[_loc6_];
         this.initTxt(param2);
         InitData.SOUNDC2.allEnd(true);
         _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[10],"M_Win2");
         InitData.SOUNDC2.addSound(_loc7_,int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
         if(ConfigAndStory.ALLPART.repairPart.length == 0)
         {
            this.repaired = true;
         }
         super(param1,param2,param3,param4,param5);
         ConfigAndStory.MONEY += ConfigAndStory.ALLPART.levelMoney;
         _mov.addEventListener(Event.ENTER_FRAME,this.missionLoop);
      }
      
      override public function addBtnL() : void
      {
         super.addBtnL();
         _mov.addEventListener(Event.ENTER_FRAME,this.missionLoop);
      }
      
      override public function removeBtnL() : void
      {
         super.removeBtnL();
         _mov.removeEventListener(Event.ENTER_FRAME,this.missionLoop);
      }
      
      private function missionLoop(param1:Event) : void
      {
         var _loc3_:Number = NaN;
         var _loc2_:Number = ConfigAndStory.ALLPART.levelMoney;
         if(this.tempMoney < _loc2_)
         {
            _loc3_ = (_loc2_ - this.tempMoney) * 0.1;
            if(_loc3_ < 1)
            {
               _loc3_ = 1;
            }
            this.tempMoney += _loc3_;
            if(this.tempMoney > _loc2_)
            {
               this.tempMoney = _loc2_;
            }
         }
         (_mov.getChildByName("txtMoney") as TextField).text = "Money: +" + int(this.tempMoney) + "G";
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "next")
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
         var tmpui:UIBase = null;
         InitData.CORE.repair.shopNewFalse();
         InitData.CORE.repair.removeParts();
         tmpui = this;
         allRemoved = function():void
         {
            ConfigAndStory.DIRECTOR.afterLevelMov(nextGame,tmpui);
         };
         removeSelf();
      }
      
      public function nextGame() : void
      {
         MakeUI.makeChoose(this,_sp,_ui);
         ConfigAndStory.saveData();
         ConfigAndStory.save();
      }
      
      public function initTxt(param1:MovieClip) : void
      {
         (param1.getChildByName("txtTime") as TextField).text = "Time: " + InitData.TARGETS.evts.nowTime;
         (param1.getChildByName("txtItem2") as TextField).text = "Parts: " + ConfigAndStory.ALLPART.repairPart.length;
         (param1.getChildByName("txtMoney") as TextField).text = "Money: +" + int(this.tempMoney) + "G";
         if(ConfigAndStory.ALLPART.repairPart.length > 0)
         {
            (param1.getChildByName("txtPerfect") as TextField).visible = false;
         }
      }
   }
}

