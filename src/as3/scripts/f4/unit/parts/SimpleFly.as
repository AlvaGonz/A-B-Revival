package f4.unit.parts
{
   import f4.data.CkBltObj;
   import f4.data.InitData;
   import f4.data.sound32.SoundUBlt32;
   import f4.unit.ai.AIFunctions;
   import f4.unit.ai.AIUnity;
   import f4.unit.move.MoveAs;
   import f4.unit.particle.Particle;
   import f4.unit.render.TRender;
   import f4.unit.swave.SWave;
   import f4data.F4DataBullet;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.tool.LineCP;
   
   public class SimpleFly extends FlyBase
   {
      
      public var life:int;
      
      public var ckRect:Rectangle;
      
      public var aim:Point;
      
      public var aif:AIFunctions;
      
      public var ckblt:CkBltObj;
      
      public var line:LineCP;
      
      public var whoFire:AIUnity;
      
      public var bulletData:F4DataBullet;
      
      public var deadSpd:Point;
      
      public function SimpleFly(param1:MoveAs, param2:F4DataBullet, param3:AIUnity)
      {
         super(param1);
         this.ckRect = new Rectangle();
         this.aif = new AIFunctions(Math.PI / 60);
         this.ckblt = new CkBltObj(this);
         this.line = new LineCP(new Point(),new Point());
         this.whoFire = param3;
         this.bulletData = param2;
      }
      
      override public function runA1() : void
      {
         move.radFace = move.spd.y;
         if(this.aim)
         {
            this.aif.closeToRad(move,this.aim,1);
            move.spd.y = move.radFace;
            move.r2d(move.spd,move.spdp);
            if(this.aim.x < -5000 && this.aim.y < -5000)
            {
               this.aim = null;
            }
         }
         move.updateSpd();
         move.runMove();
         this.makeCkRect();
         if(firePorts)
         {
            firePorts.runPorts(true,0,null);
         }
         this.ckblt.updateCk();
         outCam();
         --this.life;
         if(this.life == 0)
         {
            move.accp.x = move.accp.y = 0;
            if(move.spd.x > 30)
            {
               move.autof = 0.8;
            }
         }
         else if(this.life == -10)
         {
            isDeadA1 = true;
         }
         if(data.hp.x <= 0)
         {
            isDeadA1 = true;
         }
      }
      
      public function renderAct() : void
      {
         var _loc1_:Number = NaN;
         var _loc2_:Number = NaN;
         var _loc3_:Number = NaN;
         var _loc4_:Particle = null;
         render.runRender(0);
         if(this.life > 0 && render.inRender && Boolean(this.bulletData.partPoint))
         {
            _loc1_ = move.radFace + this.bulletData.partPoint.y;
            _loc2_ = move.point.x + this.bulletData.partPoint.x * Math.cos(_loc1_);
            _loc3_ = move.point.y + this.bulletData.partPoint.x * Math.sin(_loc1_);
            _loc4_ = new Particle(render.cam,_loc2_ - (render.cam.rect.x - render.cam.last.x),_loc3_ - (render.cam.rect.y - render.cam.last.y),Math.PI + _loc1_ + Math.PI * (Math.random() * 10 - 5) / 180,1,Math.random() + 2,30,0.95,1);
            _loc4_.layer = 1;
            _loc4_.flash = false;
            _loc4_.spd.x += move.spdp.x * 0.5;
            _loc4_.spd.y += move.spdp.y * 0.5;
            targets.pa1.addOne1(_loc4_);
         }
      }
      
      public function makeCkRect() : void
      {
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _loc1_:Number = move.spdp.x + move.dxy.x;
         var _loc2_:Number = move.spdp.y + move.dxy.y;
         if(_loc1_ >= 0)
         {
            _loc1_ = move.hitRect.x - _loc1_;
            _loc3_ = move.hitRect.x + move.hitRect.width;
         }
         else
         {
            _loc3_ = move.hitRect.x + move.hitRect.width - _loc1_;
            _loc1_ = move.hitRect.x;
         }
         if(_loc2_ >= 0)
         {
            _loc2_ = move.hitRect.y - _loc2_;
            _loc4_ = move.hitRect.y + move.hitRect.height;
         }
         else
         {
            _loc4_ = move.hitRect.y + move.hitRect.height - _loc2_;
            _loc2_ = move.hitRect.y;
         }
         this.ckRect.x = _loc1_;
         this.ckRect.y = _loc2_;
         this.ckRect.width = _loc3_ - _loc1_;
         this.ckRect.height = _loc4_ - _loc2_;
      }
      
      override public function startRemoveA1() : void
      {
         var _loc1_:Particle = null;
         var _loc2_:int = 0;
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:SWave = null;
         var _loc7_:SoundUBlt32 = null;
         if(render.inRender)
         {
            _loc2_ = -1;
            if(render is TRender)
            {
               _loc3_ = 30;
            }
            else
            {
               _loc3_ = render.h;
            }
            if(this.deadSpd)
            {
               _loc4_ = this.deadSpd.x;
               _loc5_ = this.deadSpd.y;
            }
            else
            {
               _loc4_ = move.spdp.x;
               _loc5_ = move.spdp.y;
            }
            while(++_loc2_ < this.bulletData.deadAct)
            {
               _loc1_ = new Particle(render.cam,move.point.x,move.point.y,Math.PI * 2 * Math.random(),Math.random() * 8 + 3,1,_loc3_,0.85,15);
               _loc1_.layer = 1;
               _loc1_.spd.x += _loc4_ * 0.5;
               _loc1_.spd.y += _loc5_ * 0.5;
               InitData.TARGETS.pa1.addOne1(_loc1_);
            }
         }
         if(this.bulletData.deadAct > 7)
         {
            _loc6_ = new SWave(move.point.x,move.point.y,this.bulletData.deadAct,0,this.bulletData.deadAct);
            if(this.whoFire.unity.friend)
            {
               InitData.TARGETS.swa1.addOne1(_loc6_);
            }
            else
            {
               InitData.TARGETS.swa2.addOne1(_loc6_);
            }
         }
         if(this.bulletData.deadAct >= 10)
         {
            _loc7_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[4],4,move.point);
            _loc7_.volMut = this.bulletData.deadAct < 12 ? this.bulletData.deadAct / 48 : 0.25;
            InitData.SOUNDC32.addSFX25(_loc7_);
         }
         InitData.CORE.cam.addShake(move.point.x,move.point.y,this.bulletData.deadAct / 20);
         this.whoFire = null;
         this.aim = null;
         this.ckblt = null;
         this.line = null;
         this.aif = null;
         this.deadSpd = null;
      }
   }
}

