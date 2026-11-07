package f4.unit.fire
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.data.sound32.SoundUBlt32;
   import f4.make.Maker;
   import f4.unit.ai.AIPart;
   import f4.unit.ai.AIUnity;
   import f4.unit.move.MoveAs;
   import f4.unit.particle.Particle;
   import f4.unit.parts.SimpleFly;
   import f4.unit.render.Cam;
   import f4.unit.upgrade.FlyData;
   import f4data.F4DataBullet;
   import flash.geom.Point;
   
   public class FireMode
   {
      
      public var fireTimer:FireTimer;
      
      public var fireLp:FireLoop;
      
      public var fireSg:FireSingle;
      
      public var port:FirePort;
      
      public var fireAI:String;
      
      public var fireRender:String;
      
      public var firePorts:String;
      
      public var maker:Maker;
      
      public var targets:Targets;
      
      public var part:AIPart;
      
      public var unity:AIUnity;
      
      public var friend:Boolean;
      
      private const pidface:Number = InitData.RENDERDATA.PIDFACE;
      
      public var bulletData:F4DataBullet;
      
      public var powrate:Number = 1;
      
      public var powdmg:Number = 1;
      
      public var powspd:Number = 1;
      
      public function FireMode()
      {
         super();
         this.targets = InitData.TARGETS;
      }
      
      public function runMode(param1:Boolean, param2:Number, param3:Point) : void
      {
         var _loc4_:Number = NaN;
         var _loc5_:int = 0;
         if(param1)
         {
            this.fireTimer.unFire();
            this.fireLp.reset();
         }
         else if(this.fireTimer.isFireFrame)
         {
            _loc5_ = 0;
            while(_loc5_ < this.fireSg.fireNum)
            {
               _loc4_ = Math.random() * param2 - param2 / 2;
               this.fireOne(this.fireSg.spdS + _loc5_ * this.fireSg.spdAdd + this.fireLp.spdn,_loc4_ + this.fireSg.angS + _loc5_ * this.fireSg.angAdd + this.fireLp.angn,this.fireSg.accS + _loc5_ * this.fireSg.accAdd + this.fireLp.accn,_loc4_ + this.fireSg.accAngS + _loc5_ * this.fireSg.accAngAdd + this.fireLp.accAngn,param3);
               _loc5_++;
            }
            this.fireLp.runFl();
         }
      }
      
      protected function fireOne(param1:Number, param2:Number, param3:Number, param4:Number, param5:Point) : void
      {
         var _loc12_:Particle = null;
         var _loc14_:SoundUBlt32 = null;
         var _loc15_:Number = NaN;
         var _loc16_:int = 0;
         var _loc6_:Number = Math.round((this.part.part.partMove.radFire + this.port.disRad.y) / this.pidface) * this.pidface;
         var _loc7_:Number = this.part.part.partMove.point.x + this.port.disRad.x * Math.cos(_loc6_);
         var _loc8_:Number = this.part.part.partMove.point.y + this.port.disRad.x * Math.sin(_loc6_);
         var _loc9_:Cam = InitData.CORE.cam;
         var _loc10_:Number = param2 / 180 * Math.PI;
         var _loc11_:SimpleFly = this.maker.makeFly(_loc7_ - (_loc9_.rect.x - _loc9_.last.x),_loc8_ - (_loc9_.rect.y - _loc9_.last.y),this.unity,this.bulletData,this.part.hue);
         _loc11_.move.initSpd(param1 * this.powspd,_loc10_ + this.part.part.partMove.radFire,param3 * this.powspd,param4 / 180 * Math.PI + this.part.part.partMove.radFire);
         _loc11_.move.spdp.x += this.unity.unity.move.spdp.x;
         _loc11_.move.spdp.y += this.unity.unity.move.spdp.y;
         _loc11_.data = new FlyData(this.bulletData.name,new Point(this.bulletData.hp,this.bulletData.dmg * this.powdmg),new Point(),new Point(),0,this.bulletData.mass);
         _loc11_.move.makeHitRectFromWH(this.bulletData.hit.x,this.bulletData.hit.y);
         _loc11_.render.changeColor(this.part.hue + 180);
         _loc11_.life = this.bulletData.life;
         if(this.bulletData.aim > 0 && Boolean(this.part.aim))
         {
            _loc11_.aim = this.part.aim;
            _loc11_.aif.rotLimit = (this.bulletData.aim / 180 * Math.PI) * 0.6;
         }
         if(this.friend)
         {
            this.targets.pba1.addOne1(_loc11_);
         }
         else
         {
            this.targets.eba1.addOne1(_loc11_);
         }
         var _muzzleCount:int = 4;
         if(this.targets && this.targets.pa1)
         {
            if(this.targets.pa1.num >= 350)
            {
               _muzzleCount = 0;
            }
            else if(this.targets.pa1.num >= 180)
            {
               _muzzleCount = 1;
            }
         }
         var _loc13_:int = 0;
         while(_loc13_ < _muzzleCount)
         {
            _loc12_ = new Particle(_loc9_,_loc7_ - (_loc9_.rect.x - _loc9_.last.x) - this.unity.unity.move.spdp.x,_loc8_ - (_loc9_.rect.y - _loc9_.last.y) - this.unity.unity.move.spdp.y,Math.random() * Math.PI / 6 - Math.PI / 12 + _loc10_ + this.part.part.partMove.radFire,Math.random() * 3 + 1,1,0,0.75,Math.random() * 2 + 1);
            _loc12_.spd.x = _loc12_.spd.x + this.unity.unity.move.spdp.x;
            _loc12_.spd.y += this.unity.unity.move.spdp.y;
            _loc12_.layer = 1;
            _loc12_.flash = true;
            _loc12_.color = 16777215;
            _loc12_.render();
            this.targets.pa1.addOne1(_loc12_);
            _loc13_++;
         }
         if(param1 < 5)
         {
            _loc16_ = 7;
            _loc15_ = 0.2;
         }
         else if(this.fireTimer.stopFrame < 5)
         {
            _loc16_ = 1;
            _loc15_ = 1;
         }
         else if(this.bulletData.btwh < 5)
         {
            _loc16_ = 2;
            _loc15_ = 0.7;
         }
         else if(this.bulletData.btwh < 10)
         {
            _loc16_ = 3;
            _loc15_ = 0.8;
         }
         else
         {
            _loc16_ = 0;
            _loc15_ = 1.5;
         }
         _loc14_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[_loc16_],_loc16_,new Point(_loc7_,_loc8_));
         _loc14_.volMut = _loc15_;
         InitData.SOUNDC32.addSFX25(_loc14_);
      }
   }
}

