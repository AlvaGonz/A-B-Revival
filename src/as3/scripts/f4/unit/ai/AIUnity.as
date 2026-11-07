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
      
      public static const MODE_ANCHOR:int = 0;
      public static const MODE_ESCORT:int = 1;
      public static const MODE_EVADE:int = 2;
      public static const MODE_SEEK:int = 3;
      public static const MODE_SUPPORT:int = 4;
      public static const MODE_SHIELD:int = 5;
      public static const MODE_COUNT:int = 6;
      
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
            if(this.isCaptain)
            {
               this.steerTacticalMode(_loc1_);
            }
         }
      }
      
      private function steerTacticalMode(param1:Array) : void
      {
         var _loc2_:AIUnity = this.nearestIn(param1);
         var _loc3_:AIUnity = InitData.CORE.playerFly;
         var _loc4_:Point = null;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         switch(this.tacticalMode)
         {
            case MODE_ESCORT:
               if(_loc3_)
               {
                  _loc4_ = new Point(_loc3_.unity.move.point.x - Math.cos(_loc3_.unity.move.radFace) * 80,_loc3_.unity.move.point.y - Math.sin(_loc3_.unity.move.radFace) * 80);
                  this.moveToward(_loc4_);
               }
               break;
            case MODE_EVADE:
               _loc5_ = _loc2_ ? this.distToSq(_loc2_) : Number.MAX_VALUE;
               if(_loc5_ == 0 || _loc5_ > this.unity.udata.fireRange * this.unity.udata.fireRange)
               {
                  this.aim = this.unity.move.point;
                  this.aiPow = 0;
                  this.close = 1;
               }
               else
               {
                  _loc6_ = Math.sqrt(_loc5_);
                  _loc4_ = new Point(this.unity.move.point.x + (this.unity.move.point.x - _loc2_.unity.move.point.x) / _loc6_ * 200,this.unity.move.point.y + (this.unity.move.point.y - _loc2_.unity.move.point.y) / _loc6_ * 200);
                  this.moveToward(_loc4_);
               }
               break;
            case MODE_SEEK:
               if(_loc2_)
               {
                  this.linkUnityTarget(_loc2_);
                  this.moveToward(_loc2_.unity.move.point);
               }
               break;
            case MODE_SUPPORT:
               _loc3_ = this.supportTarget(param1);
               if(_loc3_)
               {
                  this.linkUnityTarget(_loc3_);
                  this.moveToward(_loc3_.unity.move.point);
               }
               break;
            case MODE_SHIELD:
               if(_loc2_ && _loc3_)
               {
                  _loc4_ = new Point((_loc3_.unity.move.point.x + _loc2_.unity.move.point.x) * 0.5,(_loc3_.unity.move.point.y + _loc2_.unity.move.point.y) * 0.5);
                  this.moveToward(_loc4_);
               }
               break;
            default:
               if(this.spawnPoint)
               {
                  this.moveToward(this.spawnPoint);
               }
         }
      }
      
      private function moveToward(param1:Point) : void
      {
         this.aim = param1;
         var _loc2_:Number = (this.unity.move.point.x - param1.x) * (this.unity.move.point.x - param1.x) + (this.unity.move.point.y - param1.y) * (this.unity.move.point.y - param1.y);
         var _loc3_:Number = (this.unity.move.hitRect.width + this.unity.move.hitRect.height) / 2;
         _loc3_ = _loc3_ * _loc3_ * this.aimode.bodyRt2;
         // The Captain's stand-ground AI profile zeroes bodyFar.y/bodyClose.y; tactical movement owns its thrust.
         if(_loc2_ > _loc3_)
         {
            this.close = this.aimode.bodyFar.x;
            this.aiPow = this._aiLastPowMut;
         }
         else
         {
            this.close = 1;
            this.aiPow = 0;
         }
      }
      
      private function nearestIn(param1:Array) : AIUnity
      {
         var _loc2_:AIUnity = null;
         var _loc3_:AIUnity = null;
         var _loc4_:Number = Number.MAX_VALUE;
         var _loc5_:Number = NaN;
         for each(_loc3_ in param1)
         {
            if(_loc3_ != this)
            {
               _loc5_ = this.distToSq(_loc3_);
               if(_loc5_ < _loc4_)
               {
                  _loc4_ = _loc5_;
                  _loc2_ = _loc3_;
               }
            }
         }
         return _loc2_;
      }
      
      private function distToSq(param1:AIUnity) : Number
      {
         var _loc2_:Number = this.unity.move.point.x - param1.unity.move.point.x;
         var _loc3_:Number = this.unity.move.point.y - param1.unity.move.point.y;
         return _loc2_ * _loc2_ + _loc3_ * _loc3_;
      }
      
      private function supportTarget(param1:Array) : AIUnity
      {
         var _loc2_:AIUnity = null;
         var _loc3_:AIUnity = null;
         var _loc4_:Number = Number.MAX_VALUE;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         if(InitData.CORE.cam)
         {
            for each(_loc3_ in param1)
            {
               if(_loc3_ != this)
               {
                  _loc5_ = InitData.CORE.cam.crossPoint.x - _loc3_.unity.move.point.x;
                  _loc6_ = InitData.CORE.cam.crossPoint.y - _loc3_.unity.move.point.y;
                  _loc5_ = _loc5_ * _loc5_ + _loc6_ * _loc6_;
                  if(_loc5_ < _loc4_)
                  {
                     _loc4_ = _loc5_;
                     _loc2_ = _loc3_;
                  }
               }
            }
            if(_loc2_ && _loc4_ <= this.unity.udata.fireRange * this.unity.udata.fireRange)
            {
               return _loc2_;
            }
         }
         return this.nearestIn(param1);
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
         if(this.soundEngine)
         {
            this.soundEngine.isDeadA1 = true;
            this.soundEngine = null;
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

