package f4.unit.ai
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.unit.fire.FireMode;
   import f4.unit.fire.FirePort;
   import f4.unit.move.Limiter;
   import f4.unit.move.RectLines;
   import f4.unit.particle.Particle;
   import f4.unit.parts.FlyPart;
   import f4.unit.parts.SimpleFly;
   import f4.unit.render.TRender;
   import f4.unit.swave.SWave;
   import f4data.F4DataOrg;
   import flash.geom.Point;
   
   public class AIPart
   {
      
      public var part:FlyPart;
      
      public var limit:Limiter;
      
      public var aif:AIFunctions;
      
      public var targets:Targets;
      
      public var target:AIUnity;
      
      public var aim:Point;
      
      public var onLoad:Array;
      
      public var hue:int;
      
      public var lastHitWho:AIUnity;
      
      public var rectLines:RectLines;
      
      public var showLifeTime:int;
      
      public var timer:int;
      
      public var org:F4DataOrg;
      
      public var god:Boolean = false;
      
      public function AIPart(param1:FlyPart)
      {
         super();
         this.targets = InitData.TARGETS;
         this.aif = new AIFunctions();
         this.part = param1;
         this.limit = new Limiter();
         this.onLoad = [];
         this.rectLines = new RectLines();
         this.showLifeTime = 0;
      }
      
      public function runAI(param1:Point, param2:Number) : void
      {
         var _loc3_:Number = NaN;
         var _loc4_:FireMode = null;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:Number = NaN;
         if(this.part.data.turn.x > 0 && this.part.firePorts.ports.length > 0 && (this.part.firePorts.ports[0] as FirePort).fireModes.length > 0)
         {
            _loc3_ = Math.sqrt((this.aim.x - this.part.partMove.point.x) * (this.aim.x - this.part.partMove.point.x) + (this.aim.y - this.part.partMove.point.y) * (this.aim.y - this.part.partMove.point.y));
            _loc4_ = (this.part.firePorts.ports[0] as FirePort).fireModes[0];
            _loc5_ = _loc3_ / _loc4_.fireSg.spdS * 1.414;
            _loc5_ = _loc3_ / (_loc4_.fireSg.accS * _loc5_ + _loc4_.fireSg.spdS) * 1.125;
            _loc6_ = (this.aim.x + (this.target.unity.move.spdp.x - param1.x) * _loc5_ - this.part.partMove.point.x) / _loc5_;
            _loc7_ = (this.aim.y + (this.target.unity.move.spdp.y - param1.y) * _loc5_ - this.part.partMove.point.y) / _loc5_;
            _loc8_ = Math.atan2(_loc7_,_loc6_) - param2;
            this.aif.closeToRadSR(this.part.partMove,_loc8_,1);
         }
         this.part.partMove.radFire = this.part.partMove.radFace + param2;
      }
      
      public function runAIZero(param1:Number) : void
      {
         if(this.part.data.turn.x > 0)
         {
            this.aif.closeToRadSR(this.part.partMove,0,1);
         }
         this.part.partMove.radFire = this.part.partMove.radFace + param1;
      }
      
      public function runAIStc(param1:Number) : void
      {
         var _loc2_:Number = NaN;
         if(this.part.data.turn.x > 0)
         {
            _loc2_ = Math.atan2(this.aim.y - this.part.partMove.point.y,this.aim.x - this.part.partMove.point.x) - param1;
            this.aif.closeToRadSR(this.part.partMove,_loc2_,1);
         }
         this.part.partMove.radFire = this.part.partMove.radFace + param1;
      }
      
      public function runLim() : void
      {
         --this.showLifeTime;
         this.limit.runLimiter();
         this.part.partMove.dxy.x += this.limit.dxy.x;
         this.part.partMove.dxy.y += this.limit.dxy.y;
      }
      
      public function makeFireAI() : void
      {
         var _loc1_:Number = NaN;
         var _loc2_:Number = NaN;
         var _loc3_:Number = NaN;
         var _loc7_:FireMode = null;
         var _loc9_:FirePort = null;
         var _loc4_:Number = 0;
         var _loc5_:Number = 0;
         var _loc6_:Number = 0;
         var _loc8_:Number = 1;
         for each(_loc9_ in this.part.firePorts.ports)
         {
            for each(_loc7_ in _loc9_.fireModes)
            {
               _loc8_ = _loc7_.powrate;
               _loc3_ = _loc7_.fireSg.spdS + _loc7_.fireSg.spdAdd * _loc7_.fireSg.fireNum * 0.5;
               _loc2_ = _loc3_ * _loc7_.bulletData.life + 0.5 * (_loc7_.fireSg.accS + _loc7_.fireSg.accAdd * _loc7_.fireSg.fireNum * 0.5) * _loc7_.bulletData.life * _loc7_.bulletData.life;
               _loc1_ = _loc7_.bulletData.dmg * _loc7_.bulletData.hp * _loc7_.fireSg.fireNum * (_loc7_.fireTimer.fireFrame / _loc7_.fireTimer.fireEx) / (_loc7_.fireTimer.fireFrame + _loc7_.fireTimer.stopFrame);
               if(_loc4_ < _loc1_)
               {
                  _loc4_ = _loc1_;
               }
               if(_loc5_ < _loc2_)
               {
                  _loc5_ = _loc2_;
               }
               if(_loc6_ < _loc3_)
               {
                  _loc6_ = _loc3_;
               }
            }
         }
         this.part.data.fireSpd = _loc6_ * _loc8_;
         this.part.data.firePow = _loc4_ * _loc8_;
         this.part.data.fireRange = _loc5_ * _loc8_;
      }
      
      public function updateOnLoad() : void
      {
         var _loc1_:AIPart = null;
         var _loc2_:Boolean = false;
         if(this.onLoad.length > 0)
         {
            _loc2_ = false;
            for each(_loc1_ in this.onLoad)
            {
               if(_loc1_.part.data.hp.x > 0)
               {
                  _loc2_ = true;
                  return;
               }
            }
            if(!_loc2_)
            {
               this.onLoad.length = 0;
            }
         }
      }
      
      public function onLoadDead() : void
      {
         var _loc1_:AIPart = null;
         for each(_loc1_ in this.onLoad)
         {
            _loc1_.part.data.hp.x = 0;
         }
      }
      
      public function beHitBlt(param1:SimpleFly) : void
      {
         var _loc2_:Number = NaN;
         --param1.data.hp.x;
         if(!this.god)
         {
            if(this.onLoad.length > 0)
            {
               this.beHitNotDir(param1,this);
            }
            if(this.org.type == 0)
            {
               _loc2_ = 0.9 / (this.onLoad.length + 1);
               if(_loc2_ < 0.2)
               {
                  _loc2_ = 0.2;
               }
               this.part.data.hp.x -= param1.data.hp.y * _loc2_;
            }
            else
            {
               this.part.data.hp.x -= param1.data.hp.y;
            }
            this.showLifeTime = 10;
            if(this.part.data.hp.x / this.part.data.hp.y < 0.33)
            {
               (this.part.render as TRender).toDie = true;
            }
            (this.part.render as TRender).red = 3;
            this.lastHitWho = param1.whoFire;
         }
      }
      
      public function beHitSW(param1:SWave) : void
      {
         if(!this.god)
         {
            if(this.onLoad.length > 0)
            {
               this.beHitSWNotDir(param1,this);
            }
            else
            {
               if(this.org.type == 0)
               {
                  this.part.data.hp.x -= param1.pow * 0.65;
               }
               else
               {
                  this.part.data.hp.x -= param1.pow;
               }
               this.showLifeTime = 10;
               if(this.part.data.hp.x / this.part.data.hp.y < 0.33)
               {
                  (this.part.render as TRender).toDie = true;
               }
               (this.part.render as TRender).red = 3;
            }
         }
      }
      
      public function beHitSWNotDir(param1:SWave, param2:AIPart) : void
      {
         var _loc3_:AIPart = null;
         if(param2.onLoad.length > 0)
         {
            param2.part.data.hp.x -= param1.pow * 0.01;
            for each(_loc3_ in param2.onLoad)
            {
               _loc3_.beHitSWNotDir(param1,_loc3_);
            }
         }
         else
         {
            param2.part.data.hp.x -= param1.pow * 0.1;
         }
         param2.showLifeTime = 36;
         if(param2.part.data.hp.x / param2.part.data.hp.y < 0.33)
         {
            (param2.part.render as TRender).toDie = true;
         }
         (param2.part.render as TRender).red = 3;
      }
      
      public function beHitNotDir(param1:SimpleFly, param2:AIPart) : void
      {
         var _loc7_:AIPart = null;
         var _loc3_:Number = param1.move.point.x - param2.part.partMove.point.x;
         var _loc4_:Number = param1.move.point.y - param2.part.partMove.point.y;
         var _loc5_:Number = (_loc3_ < 0 ? -_loc3_ : _loc3_) + (_loc4_ < 0 ? -_loc4_ : _loc4_);
         var _loc6_:Number = 20 / _loc5_;
         if(_loc6_ > 1)
         {
            _loc6_ = 1;
         }
         if(param2.onLoad.length > 0)
         {
            for each(_loc7_ in param2.onLoad)
            {
               _loc7_.beHitNotDir(param1,_loc7_);
            }
         }
         (param2.part.render as TRender).red = 3;
         param2.lastHitWho = param1.whoFire;
      }
      
      public function hitTargetOutRange(param1:Number, param2:AIUnity) : void
      {
         var _loc3_:Number = NaN;
         var _loc4_:AIPart = null;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:Number = NaN;
         var _loc9_:Number = NaN;
         var _loc10_:Number = NaN;
         var _loc11_:Number = NaN;
         if(this.target.playerRange > 0)
         {
            this.target = null;
         }
         else if(this.part.data.firePow > 0 && !this.target.isDeadA1 && !this.target.god)
         {
            param1 += 0.1;
            _loc3_ = this.target.unity.move.spd.x + 0.1;
            _loc4_ = this.target.unity.parts[int(this.target.unity.parts.length * Math.random())] as AIPart;
            _loc6_ = this.part.data.fireRange;
            _loc7_ = (360 - this.part.data.fireacc) / 360;
            _loc8_ = _loc4_.part.partMove.point.x - this.part.partMove.point.x < 0 ? this.part.partMove.point.x - _loc4_.part.partMove.point.x : _loc4_.part.partMove.point.x - this.part.partMove.point.x;
            _loc9_ = _loc4_.part.partMove.point.y - this.part.partMove.point.y < 0 ? this.part.partMove.point.y - _loc4_.part.partMove.point.y : _loc4_.part.partMove.point.y - this.part.partMove.point.y;
            _loc5_ = (_loc8_ + _loc9_) * 0.707 + 0.1;
            _loc11_ = this.part.data.turn.x / this.part.data.mass * _loc5_ / _loc3_;
            _loc10_ = _loc6_ / _loc5_ * (this.part.data.fireSpd / (_loc3_ + param1)) * _loc11_ * _loc7_ * this.part.data.firePow * 0.02 * Math.random();
            if(_loc4_.onLoad.length == 0)
            {
               _loc4_.part.data.hp.x -= _loc10_ * 0.075;
               (_loc4_.part.render as TRender).red = 3;
               _loc4_.lastHitWho = param2;
            }
         }
      }
      
      public function autoHeal(param1:Number) : void
      {
         var _loc2_:Particle = null;
         if(Math.random() < 0.07)
         {
            _loc2_ = new Particle(this.part.render.cam,Math.random() * this.part.partMove.hitRect.width + this.part.partMove.hitRect.x,Math.random() * this.part.partMove.hitRect.height + this.part.partMove.hitRect.y,Math.random() * Math.PI * 2,Math.random() * 0.5,1,0,0.95,Math.random() * 2 + 1);
            _loc2_.flash = true;
            _loc2_.useLine = false;
            _loc2_.layer = 1;
            _loc2_.color = 16777215;
            this.targets.pa1.addOne1(_loc2_);
         }
         this.part.data.hp.x += this.part.data.hp.y * param1;
         if(this.part.data.hp.x / this.part.data.hp.y >= 0.33)
         {
            (this.part.render as TRender).toDie = false;
         }
         if(this.part.data.hp.x > this.part.data.hp.y)
         {
            this.part.data.hp.x = this.part.data.hp.y;
         }
      }
      
      public function updateHitRect(param1:Number) : void
      {
         this.part.turn4(param1);
         this.part.partMove.hitRect.x = this.part.partMove.point.x + this.part.minx;
         this.part.partMove.hitRect.y = this.part.partMove.point.y + this.part.miny;
         this.part.partMove.hitRect.width = this.part.maxx - this.part.minx;
         this.part.partMove.hitRect.height = this.part.maxy - this.part.miny;
      }
   }
}

