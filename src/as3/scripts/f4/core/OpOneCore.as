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
         if(!this.pause && !this.over)
         {
            this.playerCtrl();
            this.targets.runTargets();
            this.targets.bltHit(this.playerFly);
            this.targets.ckSWU(this.playerFly.unity,this.targets.swa2.array);
            this.targets.pa1.run1();
            if(Fly4UI.DEBUG && AbsMain.keyCtrl.isKeyDownOnce("b"))
            {
               this.targets.evts.win();
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[17]);
            }
         }
      }
      
      public function runRender() : void
      {
         if(!this.over)
         {
            this.renderData.render(this.playerFly);
         }
      }
      
      public function playerCtrl() : void
      {
         var _loc1_:SoundUBlt32 = null;
         var _loc2_:Boolean = false;
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         if(!this.playerFly.isDeadA1)
         {
            if(this._key.isKeyDownOnce("E") || this._key.isKeyDownOnce("e"))
            {
               var _capMode:* = null;
               var _gui:* = this.targets ? this.targets.evts.gameui : null;
               if(_gui)
               {
                  _capMode = _gui.pf1;
                  if(!_capMode || !_capMode.isCaptain || _capMode.unity.udata.hp.x <= 0)
                  {
                     _capMode = _gui.pf2;
                  }
               }
               if(_capMode && _capMode.isCaptain && _capMode.unity.udata.hp.x > 0)
               {
                  _capMode.tacticalMode++;
                  if(_capMode.tacticalMode >= AIUnity.MODE_COUNT)
                  {
                     _capMode.tacticalMode = AIUnity.MODE_ANCHOR;
                  }
                  _gui.showTacticalMode(_capMode.tacticalMode);
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
               }
               else if(_gui)
               {
                  _gui.showNoCaptain();
               }
            }
            if(this._key.isKeyDownOnce("X") || this._key.isKeyDownOnce("x"))
            {
               this.autoFire = !this.autoFire;
            }
            if(this._key.isKeyDownOnce("C") || this._key.isKeyDownOnce("c"))
            {
               var _curDist:int = ConfigAndStory.CONFIG && ConfigAndStory.CONFIG[6] != null ? int(ConfigAndStory.CONFIG[6]) : 0;
               _curDist = (_curDist + 1) % 3;
               ConfigAndStory.CONFIG[6] = _curDist;
               ConfigAndStory.save();
               this.renderData.setCameraDistance(_curDist);
               var _cgui:* = this.targets ? this.targets.evts.gameui : null;
               if(_cgui && _cgui.showCameraDistance)
               {
                  _cgui.showCameraDistance(_curDist);
               }
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            }
            if(this.playerFly.aiPowMut > 1)
            {
               this.renderData.sm.ctNum += this.playerFly.aiPow * 0.03;
            }
            if(this.playerCtrlMode == 0)
            {
               if(this.playerFly.unity.powNum > 0 && this.playerFly.canUse && this._key.isKeyDownOnce(" "))
               {
                  _loc1_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[6],6,this.playerFly.unity.move.point);
                  InitData.SOUNDC32.addSFX25(_loc1_);
                  this.playerFly.canUse = false;
                  this.playerFly.aiPowMut = 3;
               }
               _loc3_ = Math.atan2(this.cam.crossPoint.y - this.playerFly.unity.move.point.y,this.cam.crossPoint.x - this.playerFly.unity.move.point.x) / Math.PI * 180;
               _loc4_ = Math.sqrt((this.cam.crossPoint.x - this.playerFly.unity.move.point.x) * (this.cam.crossPoint.x - this.playerFly.unity.move.point.x) + (this.cam.crossPoint.y - this.playerFly.unity.move.point.y) * (this.cam.crossPoint.y - this.playerFly.unity.move.point.y));
               if(_loc4_ > 200)
               {
                  _loc4_ = 200;
               }
               if(_loc4_ > 100)
               {
                  this.playerFly.aiPow = (_loc4_ - 100) / 100;
                  this.targets.evts.gameui.mouseMove(true,_loc3_);
               }
               else
               {
                  this.playerFly.aiPow = 0;
                  this.targets.evts.gameui.mouseMove(false,_loc3_);
               }
               _loc2_ = this.autoFire || this._mouse.mouseIsDown;
               this.playerFly.runUnity(!_loc2_);
               this.targets.evts.gameui.mouseFire(_loc2_);
               if(!this.levelStartChangeCtrl || this._key.isKeyDownOnce("Q"))
               {
                  this.playerCtrlMode = 1;
                  this.targets.evts.gameui.changeKM();
                  this.playerFly.linkUnityTargetA(this.playerFly,this.cam.crossPoint);
                  this.cam.screenMoveNum = 2;
                  this.levelStartChangeCtrl = true;
               }
            }
            else if(this.playerCtrlMode == 1)
            {
               if(this.playerFly.unity.powNum > 0 && this.playerFly.canUse && this._key.isKeyDownOnce(" "))
               {
                  _loc1_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[6],6,this.playerFly.unity.move.point);
                  InitData.SOUNDC32.addSFX25(_loc1_);
                  this.playerFly.canUse = false;
                  this.playerFly.aiPowMut = 3;
               }
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
               this.playerFly.aiPow = this.playerKeyTw[0];
               this.playerFly.unity.move.radFace += this.playerKeyTw[1] + this.playerKeyTw[2];
               _loc2_ = this.autoFire || this._mouse.mouseIsDown;
               this.playerFly.runUnityKey(!_loc2_);
               this.targets.evts.gameui.mouseFire(_loc2_);
               if(this._key.isKeyDownOnce("Q"))
               {
                  this.playerCtrlMode = 0;
                  this.targets.evts.gameui.changeKM();
                  this.cam.screenMoveNum = 3.5;
               }
            }
            else if(this.playerCtrlMode == 2)
            {
               this.playerFly.aiPow = 0;
               _loc2_ = this.autoFire || this._mouse.mouseIsDown;
               this.playerFly.runUnityKey(!_loc2_);
               this.targets.evts.gameui.mouseFire(_loc2_);
            }
            else if(this.playerCtrlMode == 3)
            {
               this.playerFly.runAIMain();
            }
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

