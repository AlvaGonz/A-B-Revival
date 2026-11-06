package f4.unit.ai
{
   import f4.data.InitData;
   import f4.data.sound32.SoundRun1;
   import f4.unit.move.MoveAs;
   import f4.unit.particle.DeadPart;
   import f4.unit.parts.Unity;
   import f4.unit.render.AIURender;
   import f4.unit.render.Cam;
   import f4data.sdata.ConfigAndStory;
   import flash.geom.Point;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class AIUnity extends AIBase
   {
      
      public var unity:Unity;
      
      public var aim:Point;
      
      public var aimSecond:Point;
      
      public var close:int = 1;
      
      public var aiPow:Number = 1;
      
      public var aiPowMut:Number = 1;
      
      public var lastHitWho:AIUnity;
      
      public var pilotName:Array;
      
      public var aiPowTime:Number = 0;
      
      public var canUse:Boolean;
      
      public var playerRange:uint;
      
      public var aiTimer:uint;
      
      private var _rect2:Rect2;
      
      private const pidf:Number = InitData.RENDERDATA.PIDFACE;
      
      public var renderLy:AIURender;
      
      public var ignoreAI:Boolean;
      
      public var aimode:AIRunMode;
      
      public var aimodeNum:int;
      
      public var isCaptain:Boolean = false;
      
      public var tacticalMode:int = 0;
      
      public var spawnPoint:Point;
      
      public var fromDef:Boolean = true;
      
      public var god:Boolean = false;
      
      public var itemArr:Array;
      
      public var soundEngine:SoundRun1;
      
      public var nohit:Boolean;
      
      private var _aiLastPowMut:Number;
      
      public function AIUnity(param1:Unity)
      {
         super();
         this.aimode = new AIRunMode();
         this.pilotName = [];
         this._rect2 = StaticShit.RECT2;
         this.unity = param1;
         this.renderLy = new AIURender();
         this.itemArr = [];
         this.aiPowTime = 72;
         this.canUse = true;
         if(ConfigAndStory.STORY == 99)
         {
            this._aiLastPowMut = 2.5;
         }
         else
         {
            this._aiLastPowMut = 1;
         }
      }
      
      public function linkGod() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            _loc1_.god = this.god;
         }
      }
      
      public function linkUnityTargetA(param1:AIUnity, param2:Point) : void
      {
         var _loc3_:AIPart = null;
         for each(_loc3_ in this.unity.parts)
         {
            _loc3_.target = param1;
            _loc3_.aim = param2;
         }
      }
      
      public function linkUnityTarget(param1:AIUnity) : void
      {
         var _loc2_:AIPart = null;
         if(Boolean(param1) && !param1.isDeadA1)
         {
            for each(_loc2_ in this.unity.parts)
            {
               _loc2_.target = param1;
               if(param1.unity.minLifePart)
               {
                  _loc2_.aim = param1.unity.minLifePart.part.partMove.point;
               }
               else
               {
                  _loc2_.aim = (param1.unity.parts[int(param1.unity.parts.length * Math.random())] as AIPart).part.partMove.point;
               }
            }
         }
         else
         {
            for each(_loc2_ in this.unity.parts)
            {
               _loc2_.target = null;
               _loc2_.aim = null;
            }
         }
      }
      
      public function buildHitAll() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            _loc1_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
            _loc1_.updateHitRect(this.unity.move.radFace);
         }
         this.unity.buildHitRect();
      }
      
      public function runAIMain() : void
      {
         var _loc2_:AIPart = null;
         aif.updateData(this.unity.udata);
         this.unity.flyPower(this.aiPow * this.aiPowMut);
         aif.closeToRad(this.unity.move,this.aim,this.close);
         this.unity.move.updateSpd();
         this.hitWall();
         this.unity.move.runMove();
         var _loc1_:Boolean = this.playerRange == 0;
         if(_loc1_)
         {
            for each(_loc2_ in this.unity.parts)
            {
               if(_loc2_.target)
               {
                  _loc2_.hitTargetOutRange(this.unity.move.spd.x,this);
               }
               _loc2_.part.partMove.radFire = _loc2_.part.partMove.radFace + this.unity.move.radFace;
               _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
               _loc2_.updateHitRect(this.unity.move.radFace);
            }
         }
         else
         {
            for each(_loc2_ in this.unity.parts)
            {
               if(_loc2_.target)
               {
                  _loc2_.runAI(this.unity.move.spdp,this.unity.move.radFace);
                  _loc2_.runLim();
                  _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
                  if(_loc2_.org.type != 0 || this.unity.gunNum == 0)
                  {
                     _loc2_.part.firePorts.runPorts(false,_loc2_.part.data.fireacc,_loc2_.limit.dspd);
                  }
               }
               else
               {
                  _loc2_.runAIZero(this.unity.move.radFace);
                  _loc2_.runLim();
                  _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
               }
               _loc2_.updateHitRect(this.unity.move.radFace);
            }
         }
         this.unity.buildHitRect();
         this.ckPartsDead();
         this.unity.linkPart();
         this.makeFireAI();
         this.chooseAI();
         if(this.unity.udata.hp.x <= 0)
         {
            if(!isDeadA1)
            {
               this.startRemoveA1();
               isDeadA1 = true;
            }
         }
         this.runSoundEngine();
      }
      
      public function runUnity(param1:Boolean) : void
      {
         var _loc2_:AIPart = null;
         aif.updateData(this.unity.udata);
         this.runPowMut();
         this.unity.flyPower(this.aiPow * this.aiPowMut);
         aif.closeToRad(this.unity.move,this.aim,this.close);
         this.unity.move.updateSpd();
         this.hitWall();
         this.unity.move.runMove();
         for each(_loc2_ in this.unity.parts)
         {
            if(_loc2_.target)
            {
               _loc2_.runAI(this.unity.move.spdp,this.unity.move.radFace);
            }
            else
            {
               _loc2_.runAIZero(this.unity.move.radFace);
            }
            _loc2_.runLim();
            _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
            if(_loc2_.org.type != 0 || this.unity.gunNum == 0)
            {
               _loc2_.part.firePorts.runPorts(param1,_loc2_.part.data.fireacc,_loc2_.limit.dspd);
            }
            _loc2_.updateHitRect(this.unity.move.radFace);
         }
         this.unity.buildHitRect();
         this.ckPartsDead();
         this.unity.linkPart();
         this.makeFireAI();
         this.chooseAIGun();
         if(this.unity.udata.hp.x <= 0)
         {
            if(!isDeadA1)
            {
               this.startRemoveA1();
               isDeadA1 = true;
            }
         }
         this.runSoundEngine();
      }
      
      public function runUnityKey(param1:Boolean) : void
      {
         var _loc2_:AIPart = null;
         aif.updateData(this.unity.udata);
         this.runPowMut();
         this.unity.flyPower(this.aiPow * this.aiPowMut);
         this.unity.move.updateSpd();
         this.hitWall();
         this.unity.move.runMove();
         for each(_loc2_ in this.unity.parts)
         {
            _loc2_.runAIStc(this.unity.move.radFace);
            _loc2_.runLim();
            _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
            if(_loc2_.org.type != 0 || this.unity.gunNum == 0)
            {
               _loc2_.part.firePorts.runPorts(param1,_loc2_.part.data.fireacc,_loc2_.limit.dspd);
            }
            _loc2_.updateHitRect(this.unity.move.radFace);
         }
         this.unity.buildHitRect();
         this.ckPartsDead();
         this.unity.linkPart();
         this.makeFireAI();
         if(this.unity.udata.hp.x <= 0)
         {
            if(!isDeadA1)
            {
               this.startRemoveA1();
               isDeadA1 = true;
            }
         }
         this.runSoundEngine();
      }
      
      public function render() : void
      {
         this.renderLy.render(this.unity.move.radFace,this.pidf,this.aiPow,this.unity.move.spdp,targets,this.aiPowMut);
         this.showLife();
      }
      
      public function showLife() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            if(_loc1_.part.render.inRender)
            {
               if(_loc1_.showLifeTime > 0 || _loc1_.part.data.hp.x / _loc1_.part.data.hp.y < 0.3)
               {
                  _loc1_.part.render.drawLife(_loc1_.part.data.hp);
               }
            }
         }
      }
      
      public function ckPartsDead() : void
      {
         var _loc1_:AIPart = null;
         var _loc2_:int = int(this.unity.parts.length);
         var _loc3_:* = 0;
         while(_loc3_ < _loc2_)
         {
            _loc1_ = this.unity.parts[_loc3_];
            _loc1_.updateOnLoad();
            if(_loc1_.part.data.hp.x <= 0)
            {
               this.lastHitWho = _loc1_.lastHitWho;
               targets.deadParta1.addOne1(new DeadPart(_loc1_,0.2,72,this));
               if(!this.fromDef)
               {
                  if(!(ConfigAndStory.STORY == 99 && _loc1_.org.useAs == 2 && this == InitData.CORE.playerFly))
                  {
                     _loc1_.org.dead = true;
                     _loc1_.org.rpindex = ConfigAndStory.ALLPART.repairPart.length;
                     ConfigAndStory.ALLPART.repairPart.push(_loc1_.org);
                  }
               }
               if(this == InitData.CORE.playerFly && Boolean(_loc1_.org) && _loc1_.org.type != 0)
               {
                  if(Boolean(targets) && Boolean(targets.evts) && Boolean(targets.evts.gameui))
                  {
                     targets.evts.gameui.recordLostPart(_loc1_.org.type);
                  }
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[13], 0.35);
               }
               if(!this.unity.friend)
               {
                  if(Math.random() < 0.1)
                  {
                     if(ConfigAndStory.STORY == 9)
                     {
                        InitData.MAKER.makeItem(InitData.CORE.playerFly.unity.move.point.x,InitData.CORE.playerFly.unity.move.point.y,["life"]);
                     }
                     else if(ConfigAndStory.STORY != 0)
                     {
                        InitData.MAKER.makeItem(this.unity.move.point.x,this.unity.move.point.y,["life"]);
                     }
                  }
                  if(ConfigAndStory.noNext())
                  {
                     if(this.lastHitWho == InitData.CORE.playerFly)
                     {
                        InitData.MAKER.makeItem(this.unity.move.point.x,this.unity.move.point.y,["money",_loc1_.org.cost * 0.015]);
                     }
                     else
                     {
                        InitData.MAKER.makeItem(this.unity.move.point.x,this.unity.move.point.y,["money",_loc1_.org.cost * 0.0075]);
                     }
                     if(_loc1_.org.useAs == 2)
                     {
                        if(this.lastHitWho == InitData.CORE.playerFly)
                        {
                           this.itemArr.push(["money",_loc1_.org.cost * 0.02]);
                        }
                        else
                        {
                           this.itemArr.push(["money",_loc1_.org.cost * 0.0075]);
                        }
                     }
                  }
               }
               this.renderLy.deadOne(_loc1_);
               _loc1_.onLoadDead();
               this.unity.parts[_loc3_] = this.unity.parts[--_loc2_];
               _loc3_--;
            }
            _loc3_++;
         }
         this.unity.parts.length = _loc2_;
      }
      
      override public function runA1() : void
      {
         var _loc2_:AIPart = null;
         aif.updateData(this.unity.udata);
         this.unity.flyPower(this.aiPow * this.aiPowMut);
         aif.closeToRad(this.unity.move,this.aim,this.close);
         this.unity.move.updateSpd();
         this.hitWall();
         this.unity.move.runMove();
         var _loc1_:Boolean = this.playerRange == 0;
         if(_loc1_)
         {
            for each(_loc2_ in this.unity.parts)
            {
               if(_loc2_.target)
               {
                  _loc2_.hitTargetOutRange(this.unity.move.spd.x,this);
               }
               _loc2_.part.partMove.radFire = _loc2_.part.partMove.radFace + this.unity.move.radFace;
               _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
               _loc2_.updateHitRect(this.unity.move.radFace);
            }
         }
         else
         {
            for each(_loc2_ in this.unity.parts)
            {
               if(_loc2_.target)
               {
                  _loc2_.runAI(this.unity.move.spdp,this.unity.move.radFace);
                  _loc2_.runLim();
                  _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
                  if(_loc2_.org.type != 0 || this.unity.gunNum == 0)
                  {
                     _loc2_.part.firePorts.runPorts(false,_loc2_.part.data.fireacc,_loc2_.limit.dspd);
                  }
               }
               else
               {
                  _loc2_.runAIZero(this.unity.move.radFace);
                  _loc2_.runLim();
                  _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
               }
               _loc2_.updateHitRect(this.unity.move.radFace);
            }
         }
         this.unity.buildHitRect();
         this.ckPartsDead();
         this.unity.linkPart();
         this.makeFireAI();
         this.chooseAI();
         if(this.unity.udata.hp.x <= 0)
         {
            isDeadA1 = true;
         }
         this.runSoundEngine();
      }
      
      public function runNoFly() : void
      {
         var _loc2_:AIPart = null;
         aif.updateData(this.unity.udata);
         this.unity.move.updateSpd();
         this.unity.move.runMove();
         var _loc1_:Boolean = this.playerRange == 0;
         if(_loc1_)
         {
            for each(_loc2_ in this.unity.parts)
            {
               if(_loc2_.target)
               {
                  _loc2_.hitTargetOutRange(this.unity.move.spd.x,this);
               }
               _loc2_.part.partMove.radFire = _loc2_.part.partMove.radFace + this.unity.move.radFace;
               _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
               _loc2_.updateHitRect(this.unity.move.radFace);
            }
         }
         else
         {
            for each(_loc2_ in this.unity.parts)
            {
               if(_loc2_.target)
               {
                  _loc2_.runAI(this.unity.move.spdp,this.unity.move.radFace);
                  _loc2_.runLim();
                  _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
                  if(_loc2_.org.type != 0 || this.unity.gunNum == 0)
                  {
                     _loc2_.part.firePorts.runPorts(false,_loc2_.part.data.fireacc,_loc2_.limit.dspd);
                  }
               }
               else
               {
                  _loc2_.runAIZero(this.unity.move.radFace);
                  _loc2_.runLim();
                  _loc2_.part.partMove.updateUdpoint(this.unity.move.point,this.unity.move.radFace);
               }
               _loc2_.updateHitRect(this.unity.move.radFace);
            }
         }
         this.unity.buildHitRect();
         this.ckPartsDead();
         this.unity.linkPart();
         this.makeFireAI();
         this.chooseAI();
         if(this.unity.udata.hp.x <= 0)
         {
            isDeadA1 = true;
         }
      }
      
      public function initFireAI() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            _loc1_.makeFireAI();
         }
      }
      
      public function makeFireAI() : void
      {
         var _loc4_:AIPart = null;
         var _loc5_:String = null;
         var _loc1_:Number = 0;
         var _loc2_:Number = 0;
         var _loc3_:Number = 0;
         if(Boolean(this.unity.minLifePart) && this.unity.minLifePart.part.data.hp.x <= 0)
         {
            this.unity.minLifePart = null;
         }
         this.unity.udata.haveTurn = false;
         for each(_loc4_ in this.unity.parts)
         {
            if(_loc1_ < _loc4_.part.data.firePow)
            {
               _loc1_ = _loc4_.part.data.firePow;
            }
            if(_loc2_ < _loc4_.part.data.fireRange)
            {
               _loc2_ = _loc4_.part.data.fireRange;
            }
            if(_loc3_ < _loc4_.part.data.fireSpd)
            {
               _loc3_ = _loc4_.part.data.fireSpd;
            }
            if(!this.unity.minLifePart || _loc4_.onLoad.length == 0 || this.unity.minLifePart.part.data.hp.x > _loc4_.part.data.hp.x)
            {
               this.unity.minLifePart = _loc4_;
            }
            if(_loc4_.part.data.turn.x > 0)
            {
               this.unity.udata.haveTurn = true;
            }
         }
         this.unity.udata.fireSpd = _loc3_;
         this.unity.udata.firePow = _loc1_;
         this.unity.udata.fireRange = _loc2_;
         if(this.playerRange < 2)
         {
            if(this._rect2.rectIntersects(targets.playerRect,this.unity.move.hitRect))
            {
               this.playerRange = 1;
            }
            else
            {
               this.playerRange = 0;
            }
         }
         for each(_loc5_ in this.pilotName)
         {
            targets.evts.toPlaceAct(this,_loc5_);
         }
      }
      
      public function chooseAI() : void
      {
         var _loc1_:Array = null;
         var _loc2_:AIUnity = null;
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:Number = NaN;
         var _loc9_:AIUnity = null;
         var _loc10_:Number = NaN;
         var _loc11_:Boolean = false;
         var _loc12_:Number = NaN;
         var _loc13_:Number = NaN;
         var _loc14_:Number = NaN;
         if(++this.aiTimer % this.aimode.frameEx == 0)
         {
            if(this.unity.powNum == 0)
            {
               this._aiLastPowMut = 1;
            }
            if(this.unity.friend)
            {
               _loc1_ = targets.efa1.array;
            }
            else
            {
               _loc1_ = targets.pfa1.array;
            }
            _loc10_ = int.MAX_VALUE;
            _loc11_ = false;
            _loc5_ = this.unity.udata.fireRange * this.unity.udata.fireRange;
            for each(_loc2_ in _loc1_)
            {
               if(_loc2_ != this)
               {
                  _loc8_ = (this.unity.move.point.x - _loc2_.unity.move.point.x) * (this.unity.move.point.x - _loc2_.unity.move.point.x) + (this.unity.move.point.y - _loc2_.unity.move.point.y) * (this.unity.move.point.y - _loc2_.unity.move.point.y);
                  if(_loc8_ < _loc5_)
                  {
                     if(_loc8_ < _loc10_)
                     {
                        _loc10_ = _loc8_;
                        _loc9_ = _loc2_;
                     }
                  }
               }
            }
            if(!this.unity.friend)
            {
               _loc2_ = InitData.CORE.playerFly;
               _loc8_ = (this.unity.move.point.x - _loc2_.unity.move.point.x) * (this.unity.move.point.x - _loc2_.unity.move.point.x) + (this.unity.move.point.y - _loc2_.unity.move.point.y) * (this.unity.move.point.y - _loc2_.unity.move.point.y);
               if(_loc8_ < _loc5_)
               {
                  if(_loc8_ < _loc10_)
                  {
                     _loc10_ = _loc8_;
                     _loc9_ = _loc2_;
                  }
               }
            }
            if(_loc9_)
            {
               this.linkUnityTarget(_loc9_);
               if(!this.ignoreAI)
               {
                  _loc12_ = _loc10_ / _loc5_;
                  if(this.unity.gunNum == 0)
                  {
                     if(_loc12_ > 1)
                     {
                        this.close = 1;
                        this.aiPow = 1 * this._aiLastPowMut;
                     }
                     else if(_loc12_ > 0.04)
                     {
                        this.close = 1;
                        this.aiPow = 1;
                     }
                     else
                     {
                        this.close = 3;
                        this.aiPow = 1;
                     }
                  }
                  else
                  {
                     _loc3_ = this.unity.udata.movepow.x / this.unity.udata.mass - _loc9_.unity.udata.movepow.x / _loc9_.unity.udata.mass;
                     _loc4_ = this.unity.udata.turn.x / this.unity.udata.mass - _loc9_.unity.udata.turn.x / _loc9_.unity.udata.mass;
                     _loc6_ = this.unity.udata.firePow - _loc9_.unity.udata.firePow;
                     _loc7_ = this.unity.udata.hp.x - _loc9_.unity.udata.hp.x;
                     if(_loc7_ > 0 && _loc6_ > 0 && _loc4_ > 0 && _loc3_ > 0)
                     {
                        _loc11_ = true;
                     }
                     if(_loc11_)
                     {
                        if(_loc12_ > this.aimode.strong1.x)
                        {
                           this.close = this.aimode.strong1.y;
                           this.aiPow = this.aimode.strong1.width * this._aiLastPowMut;
                        }
                        else if(_loc12_ > this.aimode.strong2.x)
                        {
                           this.close = this.aimode.strong2.y;
                           this.aiPow = this.aimode.strong2.width;
                        }
                        else if(this.unity.udata.haveTurn)
                        {
                           this.close = this.aimode.strong3.x;
                           this.aiPow = this.aimode.strong3.y;
                        }
                     }
                     else if(_loc12_ > this.aimode.weak1.x)
                     {
                        this.close = this.aimode.weak1.y;
                        this.aiPow = this.aimode.weak1.width * this._aiLastPowMut;
                     }
                     else if(this.unity.udata.haveTurn && Boolean(this.aimode.weak2.x))
                     {
                        this.close = this.aimode.weak2.y;
                        this.aiPow = this.aimode.weak2.width;
                     }
                     else
                     {
                        this.close = this.aimode.weak3.x;
                        this.aiPow = this.aimode.weak3.y;
                     }
                  }
                  this.aim = _loc9_.unity.move.point;
               }
            }
            else
            {
               this.aim = this.aimSecond;
               if(this.aimSecond.x < -5000 && this.aimSecond.y < -5000)
               {
                  this.aimSecond = this.aim = this.unity.move.point.clone();
               }
               this.linkUnityTarget(null);
               _loc13_ = (this.unity.move.point.x - this.aim.x) * (this.unity.move.point.x - this.aim.x) + (this.unity.move.point.y - this.aim.y) * (this.unity.move.point.y - this.aim.y);
               _loc14_ = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
               _loc14_ = _loc14_ * _loc14_ * this.aimode.bodyRt2;
               if(_loc13_ > _loc14_)
               {
                  this.close = this.aimode.bodyFar.x;
                  this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
               }
               else
               {
                  this.close = this.aimode.bodyClose.x;
                  this.aiPow = this.aimode.bodyClose.y;
               }
            }
         }
      }
      
          if(this.isCaptain && this.tacticalMode == 1)
          {
             var _escPlayer:* = InitData.CORE.playerFly;
             if(_escPlayer && _escPlayer.unity)
             {
                var _escBehindX:Number = _escPlayer.unity.move.point.x - Math.cos(_escPlayer.unity.move.radFace) * 80;
                var _escBehindY:Number = _escPlayer.unity.move.point.y - Math.sin(_escPlayer.unity.move.radFace) * 80;
                var _escAim:Point = new Point(_escBehindX, _escBehindY);
                this.aim = _escAim;
                this.linkUnityTarget(null);
                var _escDx:Number = this.unity.move.point.x - _escAim.x;
                var _escDy:Number = this.unity.move.point.y - _escAim.y;
                var _escDist:Number = _escDx * _escDx + _escDy * _escDy;
                var _escHr:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
                _escHr = _escHr * _escHr * this.aimode.bodyRt2;
                if(_escDist > _escHr)
                {
                   this.close = this.aimode.bodyFar.x;
                   this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
                }
                else
                {
                   this.close = this.aimode.bodyClose.x;
                   this.aiPow = this.aimode.bodyClose.y;
                }
             }
          }
          if(this.isCaptain && this.tacticalMode == 2)
          {
             var _evNearest:* = null;
             var _evNd:Number = Number.MAX_VALUE;
             for each(var _evE:* in targets.efa1.array)
             {
                if(_evE != this)
                {
                   var _evDdx:Number = this.unity.move.point.x - _evE.unity.move.point.x;
                   var _evDdy:Number = this.unity.move.point.y - _evE.unity.move.point.y;
                   var _evDd:Number = _evDdx * _evDdx + _evDdy * _evDdy;
                   if(_evDd < _evNd)
                   {
                      _evNd = _evDd;
                      _evNearest = _evE;
                   }
                }
             }
             if(_evNearest)
             {
                var _evEx:Number = this.unity.move.point.x - _evNearest.unity.move.point.x;
                var _evEy:Number = this.unity.move.point.y - _evNearest.unity.move.point.y;
                var _evDist:Number = Math.sqrt(_evEx * _evEx + _evEy * _evEy);
                var _evSafe:Number = this.unity.udata.fireRange;
                if(_evDist > _evSafe || _evDist == 0)
                {
                   this.aim = this.unity.move.point.clone();
                   this.aiPow = 0;
                   this.close = 1;
                   this.linkUnityTarget(null);
                }
                else
                {
                   var _evNx:Number = _evEx / _evDist;
                   var _evNy:Number = _evEy / _evDist;
                   var _evFx:Number = this.unity.move.point.x + _evNx * 200;
                   var _evFy:Number = this.unity.move.point.y + _evNy * 200;
                   var _evAim:Point = new Point(_evFx, _evFy);
                   this.aim = _evAim;
                   this.linkUnityTarget(null);
                   var _evDx:Number = this.unity.move.point.x - _evAim.x;
                   var _evDy:Number = this.unity.move.point.y - _evAim.y;
                   var _evDist2:Number = _evDx * _evDx + _evDy * _evDy;
                   var _evHr:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
                   _evHr = _evHr * _evHr * this.aimode.bodyRt2;
                   if(_evDist2 > _evHr)
                   {
                      this.close = this.aimode.bodyFar.x;
                      this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
                   }
                   else
                   {
                      this.close = this.aimode.bodyClose.x;
                      this.aiPow = this.aimode.bodyClose.y;
                   }
                }
             }
             else
             {
                this.aim = this.unity.move.point.clone();
                this.aiPow = 0;
                this.close = 1;
                this.linkUnityTarget(null);
             }
          }
          if(this.isCaptain && this.tacticalMode == 3)
          {
             var _sdNearest:* = null;
             var _sdNd:Number = Number.MAX_VALUE;
             for each(var _sdE:* in targets.efa1.array)
             {
                if(_sdE != this)
                {
                   var _sdDx:Number = this.unity.move.point.x - _sdE.unity.move.point.x;
                   var _sdDy:Number = this.unity.move.point.y - _sdE.unity.move.point.y;
                   var _sdD:Number = _sdDx * _sdDx + _sdDy * _sdDy;
                   if(_sdD < _sdNd)
                   {
                      _sdNd = _sdD;
                      _sdNearest = _sdE;
                   }
                }
             }
             if(_sdNearest)
             {
                this.aim = _sdNearest.unity.move.point.clone();
                this.linkUnityTarget(null);
                var _sdDx2:Number = this.unity.move.point.x - this.aim.x;
                var _sdDy2:Number = this.unity.move.point.y - this.aim.y;
                var _sdDist:Number = _sdDx2 * _sdDx2 + _sdDy2 * _sdDy2;
                var _sdHr:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
                _sdHr = _sdHr * _sdHr * this.aimode.bodyRt2;
                if(_sdDist > _sdHr)
                {
                   this.close = this.aimode.bodyFar.x;
                   this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
                }
                else
                {
                   this.close = this.aimode.bodyClose.x;
                   this.aiPow = this.aimode.bodyClose.y;
                }
             }
          }
          if(this.isCaptain && this.tacticalMode == 4)
          {
             var _fsTarget:* = null;
             var _fsNd:Number = Number.MAX_VALUE;
             var _fsRefX:Number = InitData.CORE.playerFly ? InitData.CORE.playerFly.unity.move.point.x : this.unity.move.point.x;
             var _fsRefY:Number = InitData.CORE.playerFly ? InitData.CORE.playerFly.unity.move.point.y : this.unity.move.point.y;
             if(InitData.CORE.cam)
             {
                _fsRefX = InitData.CORE.cam.crossPoint.x;
                _fsRefY = InitData.CORE.cam.crossPoint.y;
             }
             for each(var _fsE:* in targets.efa1.array)
             {
                if(_fsE != this)
                {
                   var _fsDx:Number = _fsRefX - _fsE.unity.move.point.x;
                   var _fsDy:Number = _fsRefY - _fsE.unity.move.point.y;
                   var _fsD:Number = _fsDx * _fsDx + _fsDy * _fsDy;
                   if(_fsD < _fsNd)
                   {
                      _fsNd = _fsD;
                      _fsTarget = _fsE;
                   }
                }
             }
             if(!_fsTarget)
             {
                _fsNd = Number.MAX_VALUE;
                for each(var _fsE2:* in targets.efa1.array)
                {
                   if(_fsE2 != this)
                   {
                      var _fsDx2:Number = this.unity.move.point.x - _fsE2.unity.move.point.x;
                      var _fsDy2:Number = this.unity.move.point.y - _fsE2.unity.move.point.y;
                      var _fsD2:Number = _fsDx2 * _fsDx2 + _fsDy2 * _fsDy2;
                      if(_fsD2 < _fsNd)
                      {
                         _fsNd = _fsD2;
                         _fsTarget = _fsE2;
                      }
                   }
                }
             }
             if(_fsTarget)
             {
                this.aim = _fsTarget.unity.move.point.clone();
                this.linkUnityTarget(null);
                var _fsDx3:Number = this.unity.move.point.x - this.aim.x;
                var _fsDy3:Number = this.unity.move.point.y - this.aim.y;
                var _fsDist:Number = _fsDx3 * _fsDx3 + _fsDy3 * _fsDy3;
                var _fsHr:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
                _fsHr = _fsHr * _fsHr * this.aimode.bodyRt2;
                if(_fsDist > _fsHr)
                {
                   this.close = this.aimode.bodyFar.x;
                   this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
                }
                else
                {
                   this.close = this.aimode.bodyClose.x;
                   this.aiPow = this.aimode.bodyClose.y;
                }
             }
          }
          if(this.isCaptain && this.tacticalMode == 5)
          {
             var _shNearest:* = null;
             var _shNd:Number = Number.MAX_VALUE;
             for each(var _shE:* in targets.efa1.array)
             {
                if(_shE != this)
                {
                   var _shDx:Number = this.unity.move.point.x - _shE.unity.move.point.x;
                   var _shDy:Number = this.unity.move.point.y - _shE.unity.move.point.y;
                   var _shD:Number = _shDx * _shDx + _shDy * _shDy;
                   if(_shD < _shNd)
                   {
                      _shNd = _shD;
                      _shNearest = _shE;
                   }
                }
             }
             if(_shNearest && InitData.CORE.playerFly)
             {
                var _shAimX:Number = (InitData.CORE.playerFly.unity.move.point.x + _shNearest.unity.move.point.x) / 2;
                var _shAimY:Number = (InitData.CORE.playerFly.unity.move.point.y + _shNearest.unity.move.point.y) / 2;
                var _shAim:Point = new Point(_shAimX, _shAimY);
                this.aim = _shAim;
                this.linkUnityTarget(null);
                var _shDx2:Number = this.unity.move.point.x - _shAim.x;
                var _shDy2:Number = this.unity.move.point.y - _shAim.y;
                var _shDist:Number = _shDx2 * _shDx2 + _shDy2 * _shDy2;
                var _shHr:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
                _shHr = _shHr * _shHr * this.aimode.bodyRt2;
                if(_shDist > _shHr)
                {
                   this.close = this.aimode.bodyFar.x;
                   this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
                }
                else
                {
                   this.close = this.aimode.bodyClose.x;
                   this.aiPow = this.aimode.bodyClose.y;
                }
             }
          }
          if(this.isCaptain && this.tacticalMode == 0 && this.spawnPoint)
          {
             var _capDx:Number = this.unity.move.point.x - this.spawnPoint.x;
             var _capDy:Number = this.unity.move.point.y - this.spawnPoint.y;
             var _capDist:Number = _capDx * _capDx + _capDy * _capDy;
             this.aim = this.spawnPoint.clone();
             this.linkUnityTarget(null);
             var _capHr:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
             _capHr = _capHr * _capHr * this.aimode.bodyRt2;
             if(_capDist > _capHr)
             {
                this.close = this.aimode.bodyFar.x;
                this.aiPow = this.aimode.bodyFar.y * this._aiLastPowMut;
             }
             else
             {
                this.close = this.aimode.bodyClose.x;
                this.aiPow = this.aimode.bodyClose.y;
             }
          }

      public function chooseAIGun() : void
      {
         var _loc1_:Array = null;
         var _loc2_:AIUnity = null;
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _loc5_:AIUnity = null;
         var _loc6_:Number = NaN;
         if(++this.aiTimer % 10 == 0)
         {
            if(this.unity.friend)
            {
               _loc1_ = targets.efa1.array;
            }
            else
            {
               _loc1_ = targets.pfa1.array;
            }
            _loc6_ = int.MAX_VALUE;
            _loc3_ = this.unity.udata.fireRange * this.unity.udata.fireRange;
            for each(_loc2_ in _loc1_)
            {
               if(_loc2_ != this)
               {
                  _loc4_ = (this.unity.move.point.x - _loc2_.unity.move.point.x) * (this.unity.move.point.x - _loc2_.unity.move.point.x) + (this.unity.move.point.y - _loc2_.unity.move.point.y) * (this.unity.move.point.y - _loc2_.unity.move.point.y);
                  if(_loc4_ < _loc3_)
                  {
                     if(_loc4_ < _loc6_)
                     {
                        _loc6_ = _loc4_;
                        _loc5_ = _loc2_;
                     }
                  }
               }
            }
            if(_loc5_)
            {
               this.linkUnityTarget(_loc5_);
            }
            else
            {
               this.linkUnityTarget(null);
            }
         }
      }
      
      public function ckAllDead() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            if(_loc1_.part.main && _loc1_.part.data.hp.x > 0)
            {
               return;
            }
         }
         for each(_loc1_ in this.unity.parts)
         {
            _loc1_.part.data.hp.x = 0;
         }
      }
      
      public function initAI(param1:Point) : void
      {
         if(this.isCaptain && this.spawnPoint == null)
         {
            this.spawnPoint = param1.clone();
         }
         this.aimSecond = param1;
         this.aim = this.aimSecond;
         this.unity.linkPartFirst();
         this.initFireAI();
         this.makeFireAI();
         this.chooseAI();
      }
      
      override public function startRemoveA1() : void
      {
         var _loc1_:String = null;
         var _loc2_:Array = null;
         if(this.lastHitWho == null)
         {
            this.lastHitWho = this;
         }
         for each(_loc1_ in this.pilotName)
         {
            targets.evts.uDeadAct(this,_loc1_,this.lastHitWho);
            if(_loc1_ == "Captain" || _loc1_ == "Tom")
            {
               targets.evts.findPersonAndCamMove(_loc1_);
            }
         }
         if(!this.unity.friend)
         {
            for each(_loc2_ in this.itemArr)
            {
               InitData.MAKER.makeItem(this.unity.move.point.x,this.unity.move.point.y,_loc2_);
            }
         }
         if(InitData.CORE.cam.mv == this.unity.move)
         {
            InitData.CORE.cam.mv = new MoveAs(this.unity.move.point.x,this.unity.move.point.y);
         }
         this.unity.move.point.x = this.unity.move.point.y = -1000000;
      }
      
      public function initPartsRender() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            this.renderLy.addPart(_loc1_);
         }
      }
      
      public function kill() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.unity.parts)
         {
            _loc1_.part.data.hp.x = 0;
         }
      }
      
      public function hitWall() : void
      {
         var _loc1_:Cam = null;
         if(!this.nohit)
         {
            _loc1_ = InitData.CORE.cam;
            if(this.unity.move.point.x < 0)
            {
               this.unity.move.point.x = 0;
               this.unity.move.spdp.x = -0.1 * this.unity.move.spdp.x;
            }
            else if(this.unity.move.point.x > _loc1_.wh.x)
            {
               this.unity.move.point.x = _loc1_.wh.x;
               this.unity.move.spdp.x = -0.1 * this.unity.move.spdp.x;
            }
            if(this.unity.move.point.y < 0)
            {
               this.unity.move.point.y = 0;
               this.unity.move.spdp.y = -0.1 * this.unity.move.spdp.y;
            }
            else if(this.unity.move.point.y > _loc1_.wh.y)
            {
               this.unity.move.point.y = _loc1_.wh.y;
               this.unity.move.spdp.y = -0.1 * this.unity.move.spdp.y;
            }
         }
      }
      
      public function runSoundEngine() : void
      {
         this.soundEngine.volOutMut = this.aiPow * 0.2;
         if(isDeadA1)
         {
            this.soundEngine.isDeadA1 = true;
            this.soundEngine.volOutMut = 0;
         }
      }
      
      public function runPowMut() : void
      {
         if(!this.canUse)
         {
            if(this.unity.powNum > 0 && this.aiPowMut == 1)
            {
               this.aiPowTime += 0.6;
               if(this.aiPowTime > 54)
               {
                  this.aiPowTime = 54;
                  this.canUse = true;
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[20],1.15);
               }
            }
            else
            {
               --this.aiPowTime;
               if(this.aiPowTime <= 0)
               {
                  this.aiPowTime = 0;
                  this.aiPowMut = 1;
               }
            }
         }
      }
   }
}

