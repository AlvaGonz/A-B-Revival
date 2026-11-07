package f4.core
{
   import f4.data.InitData;
   import f4.data.LevelData;
   import f4.data.Targets;
   import f4.data.sound32.SoundUBlt32;
   import f4.gfx.RenderData;
   import f4.unit.ai.AIUnity;
   import f4.unit.parts.RepairPartsCtrl;
   import f4.unit.render.Cam;
   import fullshit.AbsMain;
   import fullshit.ctrl.KeyCtrl;
   import fullshit.ctrl.MouseCtrl;
   
   public class OpOneCore
   {
      
      public var renderData:RenderData;
      
      public var cam:Cam;
      
      public var playerFly:AIUnity;
      
      public var eFly:AIUnity;
      
      public var targets:Targets;
      
      private var _key:KeyCtrl = AbsMain.keyCtrl;
      
      private var _mouse:MouseCtrl = AbsMain.mouseCtrl;
      
      public var pause:Boolean = false;
      
      public var over:Boolean = true;

      public var autoFire:Boolean = false;
      
      public var levelData:LevelData;
      
      public var playerCtrlMode:int;
      
      public var repair:RepairPartsCtrl;
      
      public var playerKeyTw:Array = [0,0,0,0];
      
      public var levelStartChangeCtrl:Boolean;
      
      public var input:CombatInput = new CombatInput();
      
      public function OpOneCore()
      {
         super();
         this.levelData = new LevelData();
         this.repair = new RepairPartsCtrl();
      }
      
      public function initLv() : void
      {
         this.levelStartChangeCtrl = false;
         this.renderData = InitData.RENDERDATA;
         this.renderData.init();
         var _initDist:int = ConfigAndStory.CONFIG && ConfigAndStory.CONFIG[6] != null ? int(ConfigAndStory.CONFIG[6]) : 0;
         if(_initDist > 0)
         {
            this.renderData.setCameraDistance(_initDist);
         }
         this.targets = InitData.TARGETS;
         this.targets.reset();
         this.targets.initEvts();
      }
      
      public function runGame() : void
      {
         if(!this.pause && !this.over && this.targets != null)
         {
            if(this.playerFly != null)
            {
               this.playerCtrl();
            }
            this.targets.runTargets();
            if(this.playerFly != null && this.playerFly.unity != null)
            {
               this.targets.bltHit(this.playerFly);
               if(this.targets.swa2 != null)
               {
                  this.targets.ckSWU(this.playerFly.unity,this.targets.swa2.array);
               }
            }
            if(this.targets.pa1 != null)
            {
               this.targets.pa1.run1();
            }
            if(Fly4UI.DEBUG && AbsMain.keyCtrl && AbsMain.keyCtrl.isKeyDownOnce("b"))
            {
               if(this.targets.evts != null)
               {
                  this.targets.evts.win();
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[17]);
               }
            }
         }
      }
      
      public function runRender() : void
      {
         if(!this.over && this.renderData != null && this.playerFly != null)
         {
            this.renderData.render(this.playerFly);
         }
      }
      
      public function playerCtrl() : void
      {
         if(this.playerFly == null || this.playerFly.isDeadA1 || this.playerFly.unity == null || this.playerFly.unity.move == null)
         {
            return;
         }
         var _loc1_:SoundUBlt32 = null;
         var _loc2_:Boolean = false;
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _gui:* = this.targets && this.targets.evts ? this.targets.evts.gameui : null;
         if(this.input != null)
         {
            this.input.updateActions(this.autoFire);
            if(this.input.isTacticalModeTriggered)
            {
               var _capMode:* = null;
               if(_gui != null)
               {
                  _capMode = _gui.pf1;
                  if(!_capMode || !_capMode.isCaptain || !_capMode.unity || !_capMode.unity.udata || !_capMode.unity.udata.hp || _capMode.unity.udata.hp.x <= 0)
                  {
                     _capMode = _gui.pf2;
                  }
               }
               if(_capMode && _capMode.isCaptain && _capMode.unity && _capMode.unity.udata && _capMode.unity.udata.hp && _capMode.unity.udata.hp.x > 0)
               {
                  _capMode.tacticalMode++;
                  if(_capMode.tacticalMode >= AIUnity.MODE_COUNT)
                  {
                     _capMode.tacticalMode = AIUnity.MODE_ANCHOR;
                  }
                  if(_gui != null)
                  {
                     _gui.showTacticalMode(_capMode.tacticalMode);
                  }
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
               }
               else if(_gui != null)
               {
                  _gui.showNoCaptain();
               }
            }
            if(this.input.isAutofireTriggered)
            {
               this.autoFire = !this.autoFire;
            }
            if(this.input.isCameraDistanceTriggered)
            {
               var _curDist:int = ConfigAndStory.CONFIG && ConfigAndStory.CONFIG[6] != null ? int(ConfigAndStory.CONFIG[6]) : 0;
               _curDist = (_curDist + 1) % 3;
               ConfigAndStory.CONFIG[6] = _curDist;
               ConfigAndStory.save();
               if(this.renderData != null)
               {
                  this.renderData.setCameraDistance(_curDist);
               }
               if(_gui != null && _gui.showCameraDistance)
               {
                  _gui.showCameraDistance(_curDist);
               }
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            }
         }
         if(this.playerFly.aiPowMut > 1 && this.renderData != null && this.renderData.sm != null)
         {
            this.renderData.sm.ctNum += this.playerFly.aiPow * 0.03;
         }
         var _isBoosterActive:Boolean = this.input ? this.input.isBoosterTriggered : false;
         var _isFireActive:Boolean = this.input ? this.input.isFireHeld : (this.autoFire || (this._mouse && this._mouse.mouseIsDown));
         var _isSwitchActive:Boolean = this.input ? this.input.isModeSwitchTriggered : false;
         var _crossX:Number = this.cam && this.cam.crossPoint ? this.cam.crossPoint.x : this.playerFly.unity.move.point.x;
         var _crossY:Number = this.cam && this.cam.crossPoint ? this.cam.crossPoint.y : this.playerFly.unity.move.point.y;
         if(this.playerCtrlMode == 0)
         {
            if(this.playerFly.unity.powNum > 0 && this.playerFly.canUse && _isBoosterActive)
            {
               _loc1_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[6],6,this.playerFly.unity.move.point);
               InitData.SOUNDC32.addSFX25(_loc1_);
               this.playerFly.canUse = false;
               this.playerFly.aiPowMut = 3;
            }
            _loc3_ = Math.atan2(_crossY - this.playerFly.unity.move.point.y,_crossX - this.playerFly.unity.move.point.x) / Math.PI * 180;
            _loc4_ = Math.sqrt((_crossX - this.playerFly.unity.move.point.x) * (_crossX - this.playerFly.unity.move.point.x) + (_crossY - this.playerFly.unity.move.point.y) * (_crossY - this.playerFly.unity.move.point.y));
            if(_loc4_ > 200)
            {
               _loc4_ = 200;
            }
            if(_loc4_ > 100)
            {
               this.playerFly.aiPow = (_loc4_ - 100) / 100;
               if(_gui != null)
               {
                  _gui.mouseMove(true,_loc3_);
               }
            }
            else
            {
               this.playerFly.aiPow = 0;
               if(_gui != null)
               {
                  _gui.mouseMove(false,_loc3_);
               }
            }
            _loc2_ = _isFireActive;
            this.playerFly.runUnity(!_loc2_);
            if(_gui != null)
            {
               _gui.mouseFire(_loc2_);
            }
            if(!this.levelStartChangeCtrl || _isSwitchActive)
            {
               this.playerCtrlMode = 1;
               if(_gui != null)
               {
                  _gui.changeKM();
               }
               if(this.cam && this.cam.crossPoint)
               {
                  this.playerFly.linkUnityTargetA(this.playerFly,this.cam.crossPoint);
               }
               if(this.cam != null)
               {
                  this.cam.screenMoveNum = 2;
               }
               this.levelStartChangeCtrl = true;
            }
         }
         else if(this.playerCtrlMode == 1)
         {
            if(this.playerFly.unity.powNum > 0 && this.playerFly.canUse && _isBoosterActive)
            {
               _loc1_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[6],6,this.playerFly.unity.move.point);
               InitData.SOUNDC32.addSFX25(_loc1_);
               this.playerFly.canUse = false;
               this.playerFly.aiPowMut = 3;
            }
            if(this._key != null)
            {
               if(this._key.isKeyDown("W") || this._key.isKeyDown(KeyCtrl.UP))
               {
                  this.playerKeyTw[0] += 0.1;
                  if(this.playerKeyTw[0] > 1)
                  {
                     this.playerKeyTw[0] = 1;
                  }
               }
               else
               {
                  this.playerKeyTw[0] -= 0.1;
                  if(this.playerKeyTw[0] < 0)
                  {
                     this.playerKeyTw[0] = 0;
                  }
               }
               if(this._key.isKeyDown("A") || this._key.isKeyDown(KeyCtrl.LEFT))
               {
                  this.playerKeyTw[1] -= this.playerFly.aif.rotLimit * 0.1;
                  if(this.playerKeyTw[1] < -this.playerFly.aif.rotLimit)
                  {
                     this.playerKeyTw[1] = -this.playerFly.aif.rotLimit;
                  }
               }
               else
               {
                  this.playerKeyTw[1] += this.playerFly.aif.rotLimit * 0.1;
                  if(this.playerKeyTw[1] > 0)
                  {
                     this.playerKeyTw[1] = 0;
                  }
               }
               if(this._key.isKeyDown("D") || this._key.isKeyDown(KeyCtrl.RIGHT))
               {
                  this.playerKeyTw[2] += this.playerFly.aif.rotLimit * 0.1;
                  if(this.playerKeyTw[2] > this.playerFly.aif.rotLimit)
                  {
                     this.playerKeyTw[2] = this.playerFly.aif.rotLimit;
                  }
               }
               else
               {
                  this.playerKeyTw[2] -= this.playerFly.aif.rotLimit * 0.1;
                  if(this.playerKeyTw[2] < 0)
                  {
                     this.playerKeyTw[2] = 0;
                  }
               }
            }
            this.playerFly.aiPow = this.playerKeyTw[0];
            this.playerFly.unity.move.radFace += this.playerKeyTw[1] + this.playerKeyTw[2];
            _loc2_ = _isFireActive;
            this.playerFly.runUnityKey(!_loc2_);
            if(_gui != null)
            {
               _gui.mouseFire(_loc2_);
            }
            if(_isSwitchActive)
            {
               this.playerCtrlMode = 0;
               if(_gui != null)
               {
                  _gui.changeKM();
               }
               if(this.cam != null)
               {
                  this.cam.screenMoveNum = 3.5;
               }
            }
         }
         else if(this.playerCtrlMode == 2)
         {
            this.playerFly.aiPow = 0;
            _loc2_ = _isFireActive;
            this.playerFly.runUnityKey(!_loc2_);
            if(_gui != null)
            {
               _gui.mouseFire(_loc2_);
            }
         }
         else if(this.playerCtrlMode == 3)
         {
            this.playerFly.runAIMain();
         }
      }
      
      public function addOneRandom(param1:Boolean) : void
      {
      }
      
      public function winAct(param1:Boolean) : void
      {
         if(param1)
         {
         }
         this.over = true;
      }
   }
}

import flash.geom.Point;
import fullshit.AbsMain;
import fullshit.ctrl.KeyCtrl;
import fullshit.ctrl.MouseCtrl;

class CombatInput
{
   public var isTacticalModeTriggered:Boolean = false;
   public var isAutofireTriggered:Boolean = false;
   public var isCameraDistanceTriggered:Boolean = false;
   public var isBoosterTriggered:Boolean = false;
   public var isModeSwitchTriggered:Boolean = false;
   public var isFireHeld:Boolean = false;

   public var moveThrust:Number = 0;
   public var moveTurn:Number = 0;

   public var isTouchMode:Boolean = false;
   public var touchAim:Point = null;

   private var _key:KeyCtrl;
   private var _mouse:MouseCtrl;

   public function CombatInput()
   {
      super();
      this._key = AbsMain.keyCtrl;
      this._mouse = AbsMain.mouseCtrl;
   }

   public function updateActions(autoFireActive:Boolean) : void
   {
      this.isTacticalModeTriggered = false;
      this.isAutofireTriggered = false;
      this.isCameraDistanceTriggered = false;
      this.isBoosterTriggered = false;
      this.isModeSwitchTriggered = false;

      if(this._key)
      {
         if(this._key.isKeyDownOnce("E") || this._key.isKeyDownOnce("e"))
         {
            this.isTacticalModeTriggered = true;
         }
         if(this._key.isKeyDownOnce("X") || this._key.isKeyDownOnce("x"))
         {
            this.isAutofireTriggered = true;
         }
         if(this._key.isKeyDownOnce("C") || this._key.isKeyDownOnce("c"))
         {
            this.isCameraDistanceTriggered = true;
         }
         if(this._key.isKeyDownOnce(" "))
         {
            this.isBoosterTriggered = true;
         }
         if(this._key.isKeyDownOnce("Q") || this._key.isKeyDownOnce("q"))
         {
            this.isModeSwitchTriggered = true;
         }
      }

      this.isFireHeld = autoFireActive || (this._mouse && this._mouse.mouseIsDown);
   }

   public function triggerTacticalMode() : void
   {
      this.isTacticalModeTriggered = true;
   }

   public function triggerAutofire() : void
   {
      this.isAutofireTriggered = true;
   }

   public function triggerCameraDistance() : void
   {
      this.isCameraDistanceTriggered = true;
   }

   public function triggerBooster() : void
   {
      this.isBoosterTriggered = true;
   }

   public function triggerModeSwitch() : void
   {
      this.isModeSwitchTriggered = true;
   }

   public function setFireHeld(param1:Boolean) : void
   {
      this.isFireHeld = param1;
   }
}

