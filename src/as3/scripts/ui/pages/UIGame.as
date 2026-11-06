package ui.pages
{
   import f4.core.OpOneCore;
   import f4.data.InitData;
   import f4.evts.EvtCtrl;
   import f4.unit.ai.AIUnity;
   import f4.unit.render.Cam;
   import f4data.sdata.ConfigAndStory;
   import flash.display.Bitmap;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.text.TextField;
   import flash.ui.Mouse;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIGame extends UIBase
   {
      
      public var cam:Cam;
      
      public var eventCrtl:EvtCtrl;
      
      public var core:OpOneCore;
      
      public var lifeBar:MovieClip;
      
      public var mapPad:MovieClip;
      
      public var aiNum:MovieClip;
      
      public var f1:MovieClip;
      
      public var f2:MovieClip;
      
      public var dirs:MovieClip;
      
      public var km:MovieClip;
      
      public var moneyTxt:TextField;
      
      public var mapbmp:Bitmap;
      
      public var tempPow:Number = 0;
      
      public var tempMoney:Number = 0;
      
      public var mouse:MovieClip;
      
      public var pf1:AIUnity;
      
      public var pf2:AIUnity;
      
      public function UIGame(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         var _loc7_:SoundUnit2 = null;
         var _loc8_:SoundUnit2 = null;
         var _loc9_:SoundUnit2 = null;
         var _loc10_:SoundUnit2 = null;
         var _loc11_:SoundUnit2 = null;
         var _loc12_:SoundUnit2 = null;
         var _loc13_:SoundUnit2 = null;
         var _loc14_:Array = null;
         ConfigAndStory.save();
         Mouse.hide();
         ConfigAndStory.addMouseLeave();
         this.mouse = Attach.attach("MouseOut") as MovieClip;
         this.mouse.mouseEnabled = false;
         this.mouse.mouseChildren = false;
         if(ConfigAndStory.STORY >= 14)
         {
            this.aiNum = param2.getChildByName("ai") as MovieClip;
         }
         else
         {
            param2.getChildByName("ai").visible = false;
         }
         this.lifeBar = param2.getChildByName("life") as MovieClip;
         this.mapPad = param2.getChildByName("map") as MovieClip;
         this.f1 = param2.getChildByName("fly1") as MovieClip;
         this.f2 = param2.getChildByName("fly2") as MovieClip;
         this.dirs = param2.getChildByName("dir") as MovieClip;
         this.km = param2.getChildByName("km") as MovieClip;
         this.moneyTxt = param2.getChildByName("money") as TextField;
         this.km.gotoAndStop(1);
         this.f1.visible = false;
         this.f2.visible = false;
         if(ConfigAndStory.STORY == 9 || ConfigAndStory.STORY == 7)
         {
            (this.mouse.getChildByName("side") as MovieClip).visible = false;
            param2.getChildByName("$help").visible = false;
         }
         this.mouseFire(false);
         if(ConfigAndStory.STORY == 7 || ConfigAndStory.STORY == 49)
         {
            (this.mouse.getChildByName("mid") as MovieClip).gotoAndStop(3);
         }
         if(ConfigAndStory.STORY != 49)
         {
            param2.getChildByName("$skip").visible = false;
         }
         if(ConfigAndStory.STORY != 7)
         {
            param2.getChildByName("stand").visible = false;
         }
         if(ConfigAndStory.STORY == 9)
         {
            this.km.visible = false;
         }
         if(ConfigAndStory.STORY == 49)
         {
            ConfigAndStory.TCONFIG = ConfigAndStory.CONFIG;
            ConfigAndStory.CONFIG = [0,1,1,1,1,1];
            ConfigAndStory.makeChange();
            this.aiNum.visible = false;
            this.lifeBar.visible = false;
            this.mapPad.visible = false;
            this.f1.visible = false;
            this.f2.visible = false;
            this.dirs.visible = false;
            this.km.visible = false;
            this.moneyTxt.visible = false;
            param2.getChildByName("$help").visible = false;
            param2.getChildByName("$pause").visible = false;
            (this.mouse.getChildByName("side") as MovieClip).visible = false;
         }
         if(!ConfigAndStory.noNext())
         {
            this.moneyTxt.visible = false;
         }
         super(param1,param2,param3,param4,param5);
         this.cam = InitData.RENDERDATA.cam;
         this.eventCrtl = InitData.TARGETS.evts;
         this.core = InitData.CORE;
         this.mapbmp = new Bitmap(InitData.RENDERDATA.MINIMAP);
         var _loc6_:MovieClip = this.mapPad.getChildByName("map") as MovieClip;
         this.mapbmp.x = _loc6_.x - _loc6_.width / 2;
         this.mapbmp.y = _loc6_.y - _loc6_.height / 2;
         this.mapPad.addChild(this.mapbmp);
         InitData.SOUNDC2.allEnd(true);
         if(ConfigAndStory.STORY == 0 || ConfigAndStory.STORY == 7)
         {
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[4],"M_Game3");
            InitData.SOUNDC2.addSound(_loc7_,int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
         }
         else if(ConfigAndStory.STORY == 8)
         {
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[7],"M_Game6");
            InitData.SOUNDC2.addSound(_loc7_,int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
         }
         else if(ConfigAndStory.STORY == 9 || ConfigAndStory.STORY == 10)
         {
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[8],"M_Game7");
            InitData.SOUNDC2.addSound(_loc7_,int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
         }
         else if(ConfigAndStory.STORY == 49)
         {
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[9],"M_Credits");
            InitData.SOUNDC2.addSound(_loc7_,1,0.01,InitData.SOUNDC2.nowVol);
         }
         else if(ConfigAndStory.STORY == 34)
         {
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[6],"M_Game5");
            _loc14_ = [0,1,2,3,5,6];
            this.randomArr(_loc14_);
            _loc8_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[0]],"M_Game" + _loc14_[0]);
            _loc9_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[1]],"M_Game" + _loc14_[1]);
            _loc10_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[2]],"M_Game" + _loc14_[2]);
            _loc11_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[3]],"M_Game" + _loc14_[3]);
            _loc12_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[4]],"M_Game" + _loc14_[4]);
            _loc13_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[5]],"M_Game" + _loc14_[5]);
            _loc7_.next = _loc8_;
            _loc8_.next = _loc9_;
            _loc9_.next = _loc10_;
            _loc10_.next = _loc11_;
            _loc11_.next = _loc12_;
            _loc12_.next = _loc13_;
            _loc13_.next = _loc7_;
            InitData.SOUNDC2.addSound(_loc7_,1,0.01,InitData.SOUNDC2.nowVol);
         }
         else if(ConfigAndStory.STORY == 99)
         {
            _loc14_ = [0,1,2,3,4,5,6];
            this.randomArr(_loc14_);
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[0]],"M_Game" + _loc14_[0]);
            _loc8_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[1]],"M_Game" + _loc14_[1]);
            _loc9_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[2]],"M_Game" + _loc14_[2]);
            _loc10_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[3]],"M_Game" + _loc14_[3]);
            _loc11_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[4]],"M_Game" + _loc14_[4]);
            _loc12_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[5]],"M_Game" + _loc14_[5]);
            _loc13_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[6]],"M_Game" + _loc14_[6]);
            _loc7_.next = _loc8_;
            _loc8_.next = _loc9_;
            _loc9_.next = _loc10_;
            _loc10_.next = _loc11_;
            _loc11_.next = _loc12_;
            _loc12_.next = _loc13_;
            _loc13_.next = _loc7_;
            InitData.SOUNDC2.addSound(_loc7_,1,0.01,InitData.SOUNDC2.nowVol);
         }
         else if(ConfigAndStory.STORY < 9)
         {
            _loc14_ = [0,1,2,3];
            this.randomArr(_loc14_);
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[0]],"M_Game" + _loc14_[0]);
            _loc8_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[1]],"M_Game" + _loc14_[1]);
            _loc9_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[2]],"M_Game" + _loc14_[2]);
            _loc10_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[3]],"M_Game" + _loc14_[3]);
            _loc7_.next = _loc8_;
            _loc8_.next = _loc9_;
            _loc9_.next = _loc10_;
            _loc10_.next = _loc7_;
            InitData.SOUNDC2.addSound(_loc7_,1,0.01,InitData.SOUNDC2.nowVol);
         }
         else
         {
            _loc14_ = [0,1,2,3,6];
            this.randomArr(_loc14_);
            _loc7_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[0]],"M_Game" + _loc14_[0]);
            _loc8_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[1]],"M_Game" + _loc14_[1]);
            _loc9_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[2]],"M_Game" + _loc14_[2]);
            _loc10_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[3]],"M_Game" + _loc14_[3]);
            _loc11_ = new SoundUnit2(InitData.SOUNDDATA.musicArray[2 + _loc14_[4]],"M_Game" + _loc14_[4]);
            _loc7_.next = _loc8_;
            _loc8_.next = _loc9_;
            _loc9_.next = _loc10_;
            _loc10_.next = _loc11_;
            _loc11_.next = _loc7_;
            InitData.SOUNDC2.addSound(_loc7_,1,0.01,InitData.SOUNDC2.nowVol);
         }
      }
      
      public function mouseMove(param1:Boolean, param2:Number) : void
      {
         if(param1)
         {
            (this.mouse.getChildByName("side") as MovieClip).gotoAndStop(2);
         }
         else
         {
            (this.mouse.getChildByName("side") as MovieClip).gotoAndStop(1);
         }
         this.mouse.getChildByName("side").rotation = param2;
      }
      
      public function mouseFire(param1:Boolean) : void
      {
         if(param1)
         {
            (this.mouse.getChildByName("mid") as MovieClip).gotoAndStop(2);
         }
         else
         {
            (this.mouse.getChildByName("mid") as MovieClip).gotoAndStop(1);
         }
      }
      
      override public function afterFadeIn() : void
      {
         if(ConfigAndStory.STORY == 0)
         {
            if(ConfigAndStory.HOWTOPLAY[7] == 0)
            {
               InitData.CORE.pause = true;
               new UIHelpX2(7,"How to play",_sp,Attach.attach("UI_HelpGame") as MovieClip,uiName,_ui,this,this.resume);
            }
         }
         _mov.addChild(this.mouse);
      }
      
      public function resume() : void
      {
         InitData.CORE.pause = false;
      }
      
      public function randomArr(param1:Array) : void
      {
         var _loc2_:int = 0;
         var _loc4_:int = 0;
         var _loc3_:int = int(param1.length);
         var _loc5_:int = 0;
         while(_loc5_ < _loc3_ - 1)
         {
            _loc2_ = int(param1[_loc5_]);
            _loc4_ = Math.random() * (_loc3_ - _loc5_) + _loc5_;
            param1[_loc5_] = param1[_loc4_];
            param1[_loc4_] = _loc2_;
            _loc5_++;
         }
      }
      
      public function changeKM() : void
      {
         this.km.gotoAndStop(InitData.CORE.playerCtrlMode + 1);
         if(InitData.CORE.playerCtrlMode == 0)
         {
            (this.mouse.getChildByName("side") as MovieClip).visible = true;
         }
         else
         {
            (this.mouse.getChildByName("side") as MovieClip).visible = false;
         }
      }
      
      override protected function drawOneStart() : void
      {
         initArr();
         this.afterFadeIn();
         _mov.addChildAt(InitData.CORE.renderData.bmp,0);
         _mov.addEventListener(Event.ENTER_FRAME,this.missionLoop);
      }
      
      override public function addBtnL() : void
      {
         super.addBtnL();
         _mov.addEventListener(Event.ENTER_FRAME,this.missionLoop);
         ConfigAndStory.addMouseLeave();
         Mouse.hide();
      }
      
      override public function removeBtnL() : void
      {
         super.removeBtnL();
         _mov.removeEventListener(Event.ENTER_FRAME,this.missionLoop);
         ConfigAndStory.removeMouseLeave();
         Mouse.show();
      }
      
      private function missionLoop(param1:Event) : void
      {
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:Number = NaN;
         var _loc9_:int = 0;
         var _loc10_:Array = null;
         var _loc11_:AIUnity = null;
         var _loc2_:MovieClip = this.lifeBar.getChildByName("powb") as MovieClip;
         this.tempPow += (this.core.playerFly.aiPow * this.core.playerFly.aiPowMut - this.tempPow) * 0.1;
         if(this.core.playerFly.canUse && this.core.playerFly.unity.powNum > 0)
         {
            if(_loc2_.alpha < 1)
            {
               _loc2_.alpha += 0.1;
            }
         }
         else if(_loc2_.alpha > 0.3)
         {
            _loc2_.alpha -= 0.1;
         }
         this.lifeBar.getChildByName("hp").width = this.core.playerFly.unity.udata.hp.x / this.core.playerFly.unity.udata.hp.y * 180;
         this.lifeBar.getChildByName("pow").width = this.tempPow / 3 * 180;
         _loc2_.width = this.core.playerFly.aiPowTime / 54 * 180;
         var _loc3_:Number = ConfigAndStory.MONEY + ConfigAndStory.ALLPART.levelMoney;
         if(this.tempMoney < _loc3_)
         {
            _loc4_ = (_loc3_ - this.tempMoney) * 0.1;
            if(_loc4_ < 1)
            {
               _loc4_ = 1;
            }
            this.tempMoney += _loc4_;
            if(this.tempMoney > _loc3_)
            {
               this.tempMoney = _loc3_;
            }
         }
         this.moneyTxt.text = int(this.tempMoney) + " G";
         if(this.eventCrtl.spoint)
         {
            _loc5_ = this.eventCrtl.spoint.x - this.core.playerFly.unity.move.point.x;
            _loc6_ = this.eventCrtl.spoint.y - this.core.playerFly.unity.move.point.y;
            _loc7_ = Math.sqrt(_loc6_ * _loc6_ + _loc5_ * _loc5_);
            _loc8_ = Math.atan2(_loc6_,_loc5_) / Math.PI * 180;
            this.dirs.getChildByName("arrow").rotation = _loc8_;
            if(_loc7_ < 10000)
            {
               (this.dirs.getChildByName("txtDist") as TextField).text = int(_loc7_) + " KM";
            }
            else
            {
               (this.dirs.getChildByName("txtDist") as TextField).text = "Far away";
            }
         }
         if(this.pf1)
         {
            this.f1.getChildByName("hp").width = this.pf1.unity.udata.hp.x / this.pf1.unity.udata.hp.y * 48;
         }
         if(this.pf2)
         {
            this.f2.getChildByName("hp").width = this.pf2.unity.udata.hp.x / this.pf2.unity.udata.hp.y * 48;
         }
         if(this.aiNum)
         {
            _loc10_ = InitData.TARGETS.pfa1.array;
            for each(_loc11_ in _loc10_)
            {
               if(_loc11_.pilotName[0] == "AI_P")
               {
                  _loc9_++;
               }
            }
            (this.aiNum.getChildByName("txtNum") as TextField).text = "AI x " + _loc9_;
         }
         (this.dirs.getChildByName("txtTime") as TextField).text = this.eventCrtl.nowTime;
         this.mouse.x += (_mov.mouseX - this.mouse.x) * 0.33;
         this.mouse.y += (_mov.mouseY - this.mouse.y) * 0.33;
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "pause")
         {
            InitData.CORE.pause = true;
            new UIPause(_sp,Attach.attach("UI_Pause") as MovieClip,"UI_Pause",_ui,this);
         }
         else if(_loc2_ == "help")
         {
            InitData.CORE.pause = true;
            new UIHelpX2(7,"How to play",_sp,Attach.attach("UI_HelpGame") as MovieClip,uiName,_ui,this,this.resume);
         }
         else if(_loc2_ == "skip")
         {
            if(ConfigAndStory.STORY == 49)
            {
               InitData.TARGETS.evts.win();
            }
         }
      }
      
      public function mapBack() : void
      {
         InitData.CORE.pause = false;
      }
   }
}

