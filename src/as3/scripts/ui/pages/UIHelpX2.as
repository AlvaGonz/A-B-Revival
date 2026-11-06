package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.filters.DropShadowFilter;
   import flash.filters.GlowFilter;
   import flash.text.TextField;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIHelpX2 extends UIBase
   {
      
      public var endFunc:Function;
      
      public var helpMC:MovieClip;
      
      private var _runFlag:int;
      
      private var _turnPage:int;
      
      private var _timer:int = 0;
      
      private var _timerEnd:int;
      
      private var _title:String;
      
      public function UIHelpX2(param1:int, param2:String, param3:DisplayObjectContainer, param4:MovieClip, param5:String, param6:UICtrl, param7:UIBase = null, param8:Function = null)
      {
         param4.filters = [new GlowFilter(16776960,0.4,6,6,0.8,1)];
         this.endFunc = param8;
         this.helpMC = param4.getChildByName("help") as MovieClip;
         this.helpMC.filters = [new GlowFilter(16777215,0.3,6,6,0.8,1),new DropShadowFilter(4,45,3355443,1,6,6)];
         this._timerEnd = this.helpMC.totalFrames * 1.2 * 36;
         this.helpMC.gotoAndStop(1);
         this._title = param2;
         (param4.getChildByName("txtTitle") as TextField).text = param2;
         if(ConfigAndStory.HOWTOPLAY[param1] == 0)
         {
            ConfigAndStory.HOWTOPLAY[param1] = 1;
            ConfigAndStory.save();
         }
         _mov = param4;
         _mov.getChildByName("$next").visible = false;
         this.ckNext();
         super(param3,param4,param5,param6,param7);
         if(ConfigAndStory.CONFIG[4] == 1)
         {
            InitData.SOUNDC2.allTo(0.2,-0.05,false);
         }
         _mov.addEventListener(Event.ENTER_FRAME,this.loop);
      }
      
      private function loop(param1:Event) : void
      {
         if(this._runFlag == -1)
         {
            this.helpMC.alpha -= 0.1;
            if(this.helpMC.alpha <= 0)
            {
               this.helpMC.alpha = 0;
               this._runFlag = 1;
               if(this._turnPage == 1)
               {
                  this.helpMC.nextFrame();
               }
               else
               {
                  this.helpMC.prevFrame();
               }
               this.ckNext();
            }
         }
         else if(this._runFlag == 1)
         {
            this.helpMC.alpha += 0.1;
            if(this.helpMC.alpha >= 1)
            {
               this.helpMC.alpha = 1;
               this._runFlag = 0;
            }
         }
         ++this._timer;
      }
      
      override public function removeBtnL() : void
      {
         super.removeBtnL();
         _mov.removeEventListener(Event.ENTER_FRAME,this.loop);
      }
      
      override public function addBtnL() : void
      {
         super.addBtnL();
         _mov.addEventListener(Event.ENTER_FRAME,this.loop);
      }
      
      public function ckNext() : void
      {
         if(this.helpMC.currentFrame == 1)
         {
            _mov.getChildByName("$prev").visible = false;
         }
         else
         {
            _mov.getChildByName("$prev").visible = true;
         }
      }
      
      public function turnPage(param1:int) : void
      {
         if(this._runFlag == 0)
         {
            this._turnPage = param1;
            this._runFlag = -1;
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "big")
         {
            if(this.helpMC.currentFrame != this.helpMC.totalFrames)
            {
               this.turnPage(1);
            }
            else if(this._timer < this._timerEnd)
            {
               new UIReally(this.skipYes,null,null,"Skip tutorial?","Really wanna skip this tutorial?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
            else
            {
               this.skipYes();
            }
         }
         else if(_loc2_ == "next")
         {
            if(this.helpMC.currentFrame != this.helpMC.totalFrames)
            {
               this.turnPage(1);
            }
         }
         else if(_loc2_ == "prev")
         {
            if(this.helpMC.currentFrame != 1)
            {
               this.turnPage(-1);
            }
         }
         else if(_loc2_ == "skip")
         {
            new UIReally(this.skipYes,null,null,"Skip tutorial?","Really wanna skip this tutorial?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
      }
      
      public function skipYes() : void
      {
         if(this.endFunc != null)
         {
            this.endFunc();
         }
         InitData.SOUNDC2.allResume();
         removeSelf();
         MochiEvents.trackEvent("skip_tutorial",this._title);
      }
   }
}

