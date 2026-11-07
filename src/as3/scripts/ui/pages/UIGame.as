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
   import flash.display.Shape;
   import flash.display.Graphics;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.ColorTransform;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import flash.ui.Mouse;
   import flash.utils.getTimer;
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

      public var lostPartsTxt:TextField;
      public var lostGun:int = 0;
      public var lostWing:int = 0;
      public var lostEng:int = 0;
      public var lostMod:int = 0;
      public var lostFlashTimer:int = 0;

      public var capAlertTxt:TextField;
      public var tacticalModeTxt:TextField;
      public var capModeCodeTxt:TextField;
      public var tacticalModeFade:int = 0;
      private var _tacticalModeTitles:Array = ["ANCHOR / HOLD","ESCORT / FOLLOW","EVADE / FLEE","SEEK & DESTROY","FIRE SUPPORT","SHIELD / INTERCEPT"];
      private var _tacticalModeCodes:Array = ["AN","ES","EV","SD","FS","SH"];
      public var camDistanceTxt:TextField;
      public var camDistanceFade:int = 0;
      private var _camDistanceTitles:Array = ["[ CAM: SHORT (1.0x) ]","[ CAM: MEDIUM (1.3x) ]","[ CAM: FAR (1.5x) ]"];
      public var capAlertTimer:int = 0;
      public var capThresholdsFired:Array = [false, false, false, false];

      public var origMFaceX:Number = NaN;
      public var origMFaceY:Number = NaN;
      public var origCFaceX:Number = NaN;
      public var origCFaceY:Number = NaN;
      public var lastPlayerHp:Number = -1;
      public var lastCapHp:Number = -1;
      public var playerShakeTimer:int = 0;
      public var capShakeTimer:int = 0;

      public var autofireDot:Shape;
      
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

         this.lostPartsTxt = new TextField();
         this.lostPartsTxt.autoSize = TextFieldAutoSize.LEFT;
         this.lostPartsTxt.selectable = false;
         this.lostPartsTxt.mouseEnabled = false;
         UIBase.applyFormat(this.lostPartsTxt, UIBase.FONT_SANS, UIBase.SIZE_HUD, UIBase.COLOR_DAMAGE_CRIMSON, true, TextFieldAutoSize.LEFT);
         UIBase.applyGlow(this.lostPartsTxt, 0x000000, 0.9, 3, 3);
         this.lostPartsTxt.x = this.lifeBar.x;
         this.lostPartsTxt.y = this.lifeBar.y - 18;
         param2.addChild(this.lostPartsTxt);

         this.capAlertTxt = new TextField();
         this.capAlertTxt.autoSize = TextFieldAutoSize.CENTER;
         this.capAlertTxt.selectable = false;
         this.capAlertTxt.mouseEnabled = false;
         UIBase.applyFormat(this.capAlertTxt, UIBase.FONT_SANS, UIBase.SIZE_ALERT, UIBase.COLOR_CAPTAIN_AMBER, true, TextFormatAlign.CENTER);
         UIBase.applyHoloGlow(this.capAlertTxt, UIBase.COLOR_CAPTAIN_AMBER);
         this.capAlertTxt.x = 400;
         this.capAlertTxt.y = 48;
         this.capAlertTxt.visible = false;
         param2.addChild(this.capAlertTxt);

          this.tacticalModeTxt = new TextField();
          this.tacticalModeTxt.autoSize = TextFieldAutoSize.CENTER;
          this.tacticalModeTxt.selectable = false;
          this.tacticalModeTxt.mouseEnabled = false;
          UIBase.applyFormat(this.tacticalModeTxt, UIBase.FONT_SANS, UIBase.SIZE_ALERT, UIBase.COLOR_CAPTAIN_AMBER, true, TextFormatAlign.CENTER);
          UIBase.applyHoloGlow(this.tacticalModeTxt, UIBase.COLOR_CAPTAIN_AMBER);
          this.tacticalModeTxt.x = 320;
          this.tacticalModeTxt.y = 420;
          this.tacticalModeTxt.visible = false;
          param2.addChild(this.tacticalModeTxt);

          this.camDistanceTxt = new TextField();
          this.camDistanceTxt.autoSize = TextFieldAutoSize.CENTER;
          this.camDistanceTxt.selectable = false;
          this.camDistanceTxt.mouseEnabled = false;
          UIBase.applyFormat(this.camDistanceTxt, UIBase.FONT_SANS, UIBase.SIZE_ALERT, UIBase.COLOR_SYSTEM_CYAN, true, TextFormatAlign.CENTER);
          UIBase.applyHoloGlow(this.camDistanceTxt, UIBase.COLOR_SYSTEM_CYAN);
          this.camDistanceTxt.x = 320;
          this.camDistanceTxt.y = 398;
          this.camDistanceTxt.visible = false;
          param2.addChild(this.camDistanceTxt);

          this.capModeCodeTxt = new TextField();
          this.capModeCodeTxt.autoSize = TextFieldAutoSize.CENTER;
          this.capModeCodeTxt.selectable = false;
          this.capModeCodeTxt.mouseEnabled = false;
          UIBase.applyFormat(this.capModeCodeTxt, UIBase.FONT_SANS, UIBase.SIZE_SMALL, UIBase.COLOR_CAPTAIN_AMBER, true, TextFormatAlign.CENTER);
          this.capModeCodeTxt.visible = false;
          if(this.f1)
          {
             this.f1.addChild(this.capModeCodeTxt);
             this.capModeCodeTxt.x = 2;
             this.capModeCodeTxt.y = 2;
          }

         this.autofireDot = new Shape();
         this.autofireDot.graphics.beginFill(0x000000, 0.7);
         this.autofireDot.graphics.drawCircle(12, 12, 4);
         this.autofireDot.graphics.endFill();
         this.autofireDot.graphics.beginFill(0x00FF44, 1);
         this.autofireDot.graphics.drawCircle(12, 12, 3);
         this.autofireDot.graphics.endFill();
         this.autofireDot.visible = false;
         this.mouse.addChild(this.autofireDot);
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
         this.initTouchCombatControls(param2);
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
         this.tacticalModeFade = 0;
         this.tacticalModeTxt.visible = false;
         this.camDistanceFade = 0;
         this.camDistanceTxt.visible = false;
         if(this.pf1 && this.pf1.isCaptain)
         {
            this.capModeCodeTxt.text = this._tacticalModeCodes[AIUnity.MODE_ANCHOR];
            this.capModeCodeTxt.visible = true;
         }
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
         this.updateDamageTelemetry();
         this.updateCaptainAlerts();
         this.updateCriticalDistress();
         this.updateAutofireReticle();
         this.updateTacticalModeBadge();
         this.updateCameraDistanceBadge();
      }

      public function recordLostPart(param1:int) : void
      {
         if(param1 == 1)
         {
            this.lostWing++;
         }
         else if(param1 == 2)
         {
            this.lostEng++;
         }
         else if(param1 == 3)
         {
            this.lostGun++;
         }
         else if(param1 >= 4)
         {
            this.lostMod++;
         }
         this.lostFlashTimer = 10;
         this.updateLostPartsText();
      }

      public function showTacticalMode(param1:int) : void
      {
         if(param1 < 0 || param1 >= this._tacticalModeTitles.length)
         {
            return;
         }
         this.tacticalModeTxt.text = "[ " + this._tacticalModeTitles[param1] + " ]";
         this.tacticalModeTxt.x = 320 - this.tacticalModeTxt.width / 2;
         this.tacticalModeTxt.visible = true;
         this.tacticalModeTxt.alpha = 1;
         this.tacticalModeFade = 45;
         if(this.capModeCodeTxt)
         {
            this.capModeCodeTxt.text = this._tacticalModeCodes[param1];
            this.capModeCodeTxt.visible = true;
         }
      }

      public function updateTacticalModeBadge() : void
      {
         if(this.pf1 && this.pf1.isCaptain && !this.capModeCodeTxt.visible)
         {
            this.capModeCodeTxt.text = this._tacticalModeCodes[AIUnity.MODE_ANCHOR];
            this.capModeCodeTxt.visible = true;
         }
         if(this.tacticalModeFade > 0)
         {
            this.tacticalModeFade--;
            if(this.tacticalModeFade <= 15)
            {
               this.tacticalModeTxt.alpha = this.tacticalModeFade / 15;
            }
            if(this.tacticalModeFade == 0)
            {
               this.tacticalModeTxt.visible = false;
            }
         }
      }

      public function showCameraDistance(param1:int) : void
      {
         if(param1 < 0 || param1 >= this._camDistanceTitles.length)
         {
            param1 = 0;
         }
         this.camDistanceTxt.text = this._camDistanceTitles[param1];
         this.camDistanceTxt.x = 320 - this.camDistanceTxt.width / 2;
         this.camDistanceTxt.visible = true;
         this.camDistanceTxt.alpha = 1;
         this.camDistanceFade = 45;
      }

      public function updateCameraDistanceBadge() : void
      {
         if(this.camDistanceFade > 0)
         {
            this.camDistanceFade--;
            if(this.camDistanceFade <= 15)
            {
               this.camDistanceTxt.alpha = this.camDistanceFade / 15;
            }
            if(this.camDistanceFade == 0)
            {
               this.camDistanceTxt.visible = false;
            }
         }
      }

      public function showNoCaptain() : void
      {
         this.tacticalModeTxt.text = "[ NO CAPTAIN! ]";
         this.tacticalModeTxt.x = 320 - this.tacticalModeTxt.width / 2;
         this.tacticalModeTxt.visible = true;
         this.tacticalModeTxt.alpha = 1;
         this.tacticalModeFade = 45;
      }

      public function updateLostPartsText() : void
      {
         if(!this.lostPartsTxt)
         {
            return;
         }
         var _parts:Array = [];
         if(this.lostGun > 0)
         {
            _parts.push("[Gun x" + this.lostGun + "]");
         }
         if(this.lostWing > 0)
         {
            _parts.push("[Wing x" + this.lostWing + "]");
         }
         if(this.lostEng > 0)
         {
            _parts.push("[Engine x" + this.lostEng + "]");
         }
         if(this.lostMod > 0)
         {
            _parts.push("[Module x" + this.lostMod + "]");
         }
         if(_parts.length > 0)
         {
            this.lostPartsTxt.text = "LOST: " + _parts.join(" ");
         }
         else
         {
            this.lostPartsTxt.text = "";
         }
      }

      public function triggerCaptainAlert(param1:String, param2:uint) : void
      {
         if(!this.capAlertTxt)
         {
            return;
         }
         this.capAlertTxt.text = param1;
         this.capAlertTxt.textColor = param2;
         this.capAlertTxt.x = 400 - this.capAlertTxt.width / 2;
         this.capAlertTimer = 72;
         this.capAlertTxt.visible = true;
         this.capAlertTxt.alpha = 1;
         InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[13], 0.35);
      }

      public function updateDamageTelemetry() : void
      {
         if(!this.lostPartsTxt)
         {
            return;
         }
         if(this.lostFlashTimer > 0)
         {
            this.lostFlashTimer--;
            this.lostPartsTxt.textColor = UIBase.COLOR_WHITE;
         }
         else
         {
            this.lostPartsTxt.textColor = UIBase.COLOR_DAMAGE_CRIMSON;
         }
      }

      public function updateCaptainAlerts() : void
      {
         if(Boolean(this.pf1) && Boolean(this.pf1.unity) && Boolean(this.pf1.unity.udata) && this.pf1.unity.udata.hp.y > 0)
         {
            var _capPct:Number = (this.pf1.unity.udata.hp.x / this.pf1.unity.udata.hp.y) * 100;
            if(_capPct <= 10 && !this.capThresholdsFired[3])
            {
               this.triggerCaptainAlert("[ EMERGENCY: CAPTAIN TOM FAILING - 10%! ]", 0xFF2222);
               this.capThresholdsFired[3] = true;
               this.capThresholdsFired[2] = true;
               this.capThresholdsFired[1] = true;
               this.capThresholdsFired[0] = true;
            }
            else if(_capPct <= 30 && !this.capThresholdsFired[2])
            {
               this.triggerCaptainAlert("[ WARNING: CAPTAIN TOM CRITICAL - 30% ]", 0xFF6600);
               this.capThresholdsFired[2] = true;
               this.capThresholdsFired[1] = true;
               this.capThresholdsFired[0] = true;
            }
            else if(_capPct <= 50 && !this.capThresholdsFired[1])
            {
               this.triggerCaptainAlert("[ CAPTAIN TOM: 50% HEALTH ]", 0xFFFF00);
               this.capThresholdsFired[1] = true;
               this.capThresholdsFired[0] = true;
            }
            else if(_capPct <= 70 && !this.capThresholdsFired[0])
            {
               this.triggerCaptainAlert("[ CAPTAIN TOM: 70% HEALTH ]", 0x66FF66);
               this.capThresholdsFired[0] = true;
            }
            else if(_capPct > 70)
            {
               this.capThresholdsFired[0] = false;
               this.capThresholdsFired[1] = false;
               this.capThresholdsFired[2] = false;
               this.capThresholdsFired[3] = false;
            }
         }
         if(this.capAlertTimer > 0)
         {
            this.capAlertTimer--;
            this.capAlertTxt.visible = true;
            if(this.capAlertTimer < 20)
            {
               this.capAlertTxt.alpha = this.capAlertTimer / 20;
            }
            else
            {
               this.capAlertTxt.alpha = 1;
            }
         }
         else if(this.capAlertTxt)
         {
            this.capAlertTxt.visible = false;
         }
      }

      public function updateCriticalDistress() : void
      {
         var _pulse:Number = (Math.sin(getTimer() * 0.008) + 1) * 0.5;
         
         if(Boolean(this.core) && Boolean(this.core.playerFly) && Boolean(this.core.playerFly.unity) && Boolean(this.core.playerFly.unity.udata) && this.core.playerFly.unity.udata.hp.y > 0)
         {
            var _mFace:MovieClip = this.lifeBar.getChildByName("face") as MovieClip;
            if(_mFace)
            {
               if(isNaN(this.origMFaceX))
               {
                  this.origMFaceX = _mFace.x;
                  this.origMFaceY = _mFace.y;
               }
               var _curHp:Number = this.core.playerFly.unity.udata.hp.x;
               var _mPct:Number = (_curHp / this.core.playerFly.unity.udata.hp.y) * 100;
               
               if(_mPct <= 30)
               {
                  _mFace.transform.colorTransform = new ColorTransform(1, 1 - _pulse * 0.55, 1 - _pulse * 0.55, 1, _pulse * 100, 0, 0);
                  if(this.lastPlayerHp >= 0 && _curHp < this.lastPlayerHp)
                  {
                     this.playerShakeTimer = 6;
                  }
                  if(this.playerShakeTimer > 0)
                  {
                     this.playerShakeTimer--;
                     _mFace.x = this.origMFaceX + (Math.random() * 4 - 2);
                     _mFace.y = this.origMFaceY + (Math.random() * 4 - 2);
                  }
                  else
                  {
                     _mFace.x = this.origMFaceX;
                     _mFace.y = this.origMFaceY;
                  }
               }
               else
               {
                  _mFace.transform.colorTransform = new ColorTransform();
                  _mFace.x = this.origMFaceX;
                  _mFace.y = this.origMFaceY;
               }
               this.lastPlayerHp = _curHp;
            }
         }
         
         if(Boolean(this.pf1) && Boolean(this.pf1.unity) && Boolean(this.pf1.unity.udata) && this.pf1.unity.udata.hp.y > 0 && Boolean(this.f1) && this.f1.visible)
         {
            var _cFace:MovieClip = this.f1.getChildByName("face") as MovieClip;
            if(_cFace)
            {
               if(isNaN(this.origCFaceX))
               {
                  this.origCFaceX = _cFace.x;
                  this.origCFaceY = _cFace.y;
               }
               var _curCapHp:Number = this.pf1.unity.udata.hp.x;
               var _cPct:Number = (_curCapHp / this.pf1.unity.udata.hp.y) * 100;
               
               if(_cPct <= 30)
               {
                  _cFace.transform.colorTransform = new ColorTransform(1, 1 - _pulse * 0.55, 1 - _pulse * 0.55, 1, _pulse * 100, 0, 0);
                  if(this.lastCapHp >= 0 && _curCapHp < this.lastCapHp)
                  {
                     this.capShakeTimer = 6;
                  }
                  if(this.capShakeTimer > 0)
                  {
                     this.capShakeTimer--;
                     _cFace.x = this.origCFaceX + (Math.random() * 4 - 2);
                     _cFace.y = this.origCFaceY + (Math.random() * 4 - 2);
                  }
                  else
                  {
                     _cFace.x = this.origCFaceX;
                     _cFace.y = this.origCFaceY;
                  }
               }
               else
               {
                  _cFace.transform.colorTransform = new ColorTransform();
                  _cFace.x = this.origCFaceX;
                  _cFace.y = this.origCFaceY;
               }
               this.lastCapHp = _curCapHp;
            }
         }
      }

      public function updateAutofireReticle() : void
      {
         if(Boolean(this.autofireDot) && Boolean(this.core))
         {
            this.autofireDot.visible = this.core.autoFire;
         }
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

      override public function removeSelf() : UIBase
      {
         if(InitData.CORE)
         {
            InitData.CORE.pause = true;
            InitData.CORE.over = true;
         }
         if(InitData.RENDERDATA)
         {
            InitData.RENDERDATA.disBmpds();
         }
         if(InitData.TARGETS)
         {
            InitData.TARGETS.reset();
         }
         if(this.autofireDot && this.autofireDot.parent)
         {
            this.autofireDot.parent.removeChild(this.autofireDot);
         }
         this.autofireDot = null;
         if(this._touchPillsContainer && this._touchPillsContainer.parent)
         {
            this._touchPillsContainer.parent.removeChild(this._touchPillsContainer);
         }
         this._touchPillsContainer = null;
         this.mapbmp = null;
         this.pf1 = null;
         this.pf2 = null;
         this.core = null;
         this.cam = null;
         this.eventCrtl = null;
         return super.removeSelf();
      }

      private var _touchPillsContainer:Sprite;

      private function initTouchCombatControls(param2:MovieClip) : void
      {
         this._touchPillsContainer = new Sprite();
         this._touchPillsContainer.name = "touchPills";
         param2.addChild(this._touchPillsContainer);

         this.createTouchButton("btnTouchAuto", "AUTO", 540, 24, 46, 22, function():void {
            if(InitData.CORE && InitData.CORE.input)
            {
               InitData.CORE.input.triggerAutofire();
            }
         });
         
         this.createTouchButton("btnTouchCam", "CAM", 100, 24, 44, 22, function():void {
            if(InitData.CORE && InitData.CORE.input)
            {
               InitData.CORE.input.triggerCameraDistance();
            }
         });

         this.createTouchButton("btnTouchBoost", "BOOST", 30, 440, 54, 26, function():void {
            if(InitData.CORE && InitData.CORE.input)
            {
               InitData.CORE.input.triggerBooster();
            }
         });

         this.createTouchButton("btnTouchMode", "MODE", 610, 440, 50, 26, function():void {
            if(InitData.CORE && InitData.CORE.input)
            {
               InitData.CORE.input.triggerTacticalMode();
            }
         });
      }

      private function createTouchButton(btnName:String, labelText:String, posX:Number, posY:Number, btnW:Number, btnH:Number, onTrigger:Function) : Sprite
      {
         var btn:Sprite = new Sprite();
         btn.name = btnName;
         btn.x = posX;
         btn.y = posY;
         btn.buttonMode = true;
         btn.useHandCursor = true;

         var g:Graphics = btn.graphics;
         g.beginFill(0x1a233a, 0.7);
         g.lineStyle(1, 0x4a75a0, 0.85);
         g.drawRoundRect(-btnW / 2, -btnH / 2, btnW, btnH, 6, 6);
         g.endFill();

         var tf:TextField = new TextField();
         tf.width = btnW;
         tf.height = btnH;
         tf.x = -btnW / 2;
         tf.y = -btnH / 2 + 3;
         tf.selectable = false;
         tf.mouseEnabled = false;
         UIBase.applyFormat(tf, UIBase.FONT_BODY, 10, UIBase.COLOR_WHITE, true, TextFormatAlign.CENTER);
         btn.addChild(tf);
         tf.text = labelText;

         btn.addEventListener(MouseEvent.MOUSE_DOWN, function(e:MouseEvent):void {
            e.stopPropagation();
            onTrigger();
         });

         this._touchPillsContainer.addChild(btn);
         return btn;
      }
   }
}

