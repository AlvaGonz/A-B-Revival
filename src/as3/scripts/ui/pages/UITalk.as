package ui.pages
{
   import f4.data.InitData;
   import f4.evts.EvtCtrl;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UITalk extends UIBase
   {
      
      public var endFunc:Function;
      
      public var talks:Array;
      
      public var index:int;
      
      public var talkMC:MovieClip;
      
      public var evtCtrl:EvtCtrl;
      
      public function UITalk(param1:Function, param2:Array, param3:DisplayObjectContainer, param4:MovieClip, param5:String, param6:UICtrl, param7:UIBase = null, param8:EvtCtrl = null)
      {
         this.endFunc = param1;
         _mov = param4;
         this.talkMC = param4.getChildByName("talk") as MovieClip;
         this.initTalks(param2);
         this.evtCtrl = param8;
         this.nextWord();
         if(this.talks.length <= 1)
         {
            param4.getChildByName("$skip").visible = false;
         }
         super(param3,param4,param5,param6,param7);
         if(ConfigAndStory.CONFIG[4] == 1)
         {
            InitData.SOUNDC2.allTo(0.2,-0.05,false);
         }
      }
      
      public function initTalks(param1:Array) : void
      {
         var _loc5_:MovieClip = null;
         this.talks = [];
         var _loc2_:int = int(param1[0]);
         var _loc3_:int = int(param1[1]);
         var _loc4_:int = _loc2_;
         while(_loc4_ <= _loc3_)
         {
            this.talks.push(ConfigAndStory.ALLPART.talk.talkData[_loc4_]);
            _loc4_++;
         }
         this.index = 0;
         if(param1[2])
         {
            _loc5_ = Attach.attach(param1[2]) as MovieClip;
            _mov.addChildAt(_loc5_,0);
         }
      }
      
      public function nextWord() : void
      {
         var _loc1_:Object = null;
         if(this.index < this.talks.length)
         {
            _loc1_ = this.talks[this.index];
            (this.talkMC.getChildByName("face") as MovieClip).gotoAndStop(_loc1_["who"]);
            (this.talkMC.getChildByName("txtName") as TextField).text = _loc1_["who"];
            (this.talkMC.getChildByName("txtWord") as TextField).text = _loc1_["word"];
            ++this.index;
            if(this.evtCtrl)
            {
               this.evtCtrl.findPersonAndCamMove(_loc1_["who"]);
            }
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[15]);
         }
         else
         {
            removeSelf();
            if(this.evtCtrl)
            {
               this.evtCtrl.camPlayer();
            }
            if(this.endFunc != null)
            {
               this.endFunc();
            }
            InitData.SOUNDC2.allResume();
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "big")
         {
            this.nextWord();
         }
         else if(_loc2_ == "skip")
         {
            removeSelf();
            if(this.evtCtrl)
            {
               this.evtCtrl.camPlayer();
            }
            if(this.endFunc != null)
            {
               this.endFunc();
            }
            MochiEvents.trackEvent("skip_talk",ConfigAndStory.STORY);
            InitData.SOUNDC2.allResume();
         }
      }
   }
}

