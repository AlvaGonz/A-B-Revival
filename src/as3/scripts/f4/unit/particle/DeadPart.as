package f4.unit.particle
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.data.sound32.SoundUBlt32;
   import f4.unit.ai.AIPart;
   import f4.unit.ai.AIUnity;
   import f4.unit.swave.SWave;
   import flash.geom.Point;
   import fullshit.ds.AbsOne;
   
   public class DeadPart extends AbsOne
   {
      
      public var rate:Number;
      
      public var part:AIPart;
      
      public var life:int;
      
      public var friend:Boolean;
      
      public function DeadPart(param1:AIPart, param2:Number, param3:int, param4:AIUnity)
      {
         super();
         this.friend = param4.unity.friend;
         this.rate = param2;
         this.part = param1;
         this.life = param3;
         this.part.part.partMove.autof = param4.unity.move.autof;
         this.part.part.partMove.spdp.x = param4.unity.move.spdp.x;
         this.part.part.partMove.spdp.y = param4.unity.move.spdp.y;
         this.part.part.partMove.radFace += param4.unity.move.radFace;
      }
      
      override public function runA1() : void
      {
         var _loc1_:Particle = null;
         var _loc2_:int = 0;
         var _loc3_:Targets = null;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:SoundUBlt32 = null;
         --this.life;
         if(this.life <= 0)
         {
            isDeadA1 = true;
         }
         else if(Math.random() < this.rate)
         {
            _loc2_ = -1;
            _loc3_ = InitData.TARGETS;
            _loc4_ = this.part.part.partMove.hitRect.x + Math.random() * this.part.part.partMove.hitRect.width;
            _loc5_ = this.part.part.partMove.hitRect.y + Math.random() * this.part.part.partMove.hitRect.height;
            while(++_loc2_ < 8)
            {
               _loc1_ = new Particle(this.part.part.render.cam,_loc4_,_loc5_,Math.PI * 2 * Math.random(),Math.random() * 10,Math.random() + 1,30,0.8,Math.random() * 10 + 5);
               _loc1_.spd.x += this.part.part.partMove.spdp.x;
               _loc1_.spd.y += this.part.part.partMove.spdp.y;
               _loc1_.layer = 1;
               _loc3_.pa1.addOne1(_loc1_);
            }
            if(Math.random() < 0.65)
            {
               _loc7_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[4],4,new Point(_loc4_,_loc5_));
               _loc7_.volMut = 0.4;
               InitData.SOUNDC32.addSFX25(_loc7_);
            }
            _loc6_ = this.part.part.partMove.hitRect.width + this.part.part.partMove.hitRect.height;
            InitData.CORE.cam.addShake(this.part.part.partMove.point.x,this.part.part.partMove.point.y,_loc6_ * 0.01);
         }
      }
      
      public function render() : void
      {
         this.part.part.partMove.updateSpd();
         this.part.part.partMove.runMove();
         this.part.part.render.render(0);
      }
      
      override public function startRemoveA1() : void
      {
         var _loc1_:Particle = null;
         var _loc2_:int = -1;
         var _loc3_:Targets = InitData.TARGETS;
         var _loc4_:Number = this.part.part.partMove.hitRect.width * this.part.part.partMove.hitRect.height / 60;
         if(_loc4_ < 5)
         {
            _loc4_ = 5;
         }
         else if(_loc4_ > 30)
         {
            _loc4_ = 30;
         }
         var _loc5_:Number = this.part.part.partMove.hitRect.x + Math.random() * this.part.part.partMove.hitRect.width;
         var _loc6_:Number = this.part.part.partMove.hitRect.y + Math.random() * this.part.part.partMove.hitRect.height;
         while(++_loc2_ < 20)
         {
            _loc1_ = new Particle(this.part.part.render.cam,_loc5_,_loc6_,Math.PI * 2 * Math.random(),Math.random() * _loc4_,Math.random() + 1,30,0.88,Math.random() * 10 + 10);
            _loc1_.spd.x += this.part.part.partMove.spdp.x;
            _loc1_.spd.y += this.part.part.partMove.spdp.y;
            _loc1_.layer = 1;
            _loc3_.pa1.addOne1(_loc1_);
         }
         _loc5_ = this.part.part.partMove.hitRect.x + Math.random() * this.part.part.partMove.hitRect.width;
         _loc6_ = this.part.part.partMove.hitRect.y + Math.random() * this.part.part.partMove.hitRect.height;
         while(++_loc2_ < 20)
         {
            _loc1_ = new Particle(this.part.part.render.cam,_loc5_,_loc6_,Math.PI * 2 * Math.random(),Math.random() * _loc4_,Math.random() + 1,30,0.88,Math.random() * 10 + 10);
            _loc1_.spd.x += this.part.part.partMove.spdp.x;
            _loc1_.spd.y += this.part.part.partMove.spdp.y;
            _loc1_.layer = 1;
            _loc3_.pa1.addOne1(_loc1_);
         }
         _loc5_ = this.part.part.partMove.hitRect.x + Math.random() * this.part.part.partMove.hitRect.width;
         _loc6_ = this.part.part.partMove.hitRect.y + Math.random() * this.part.part.partMove.hitRect.height;
         while(++_loc2_ < 20)
         {
            _loc1_ = new Particle(this.part.part.render.cam,_loc5_,_loc6_,Math.PI * 2 * Math.random(),Math.random() * _loc4_,Math.random() + 1,30,0.88,Math.random() * 10 + 10);
            _loc1_.spd.x += this.part.part.partMove.spdp.x;
            _loc1_.spd.y += this.part.part.partMove.spdp.y;
            _loc1_.layer = 1;
            _loc3_.pa1.addOne1(_loc1_);
         }
         var _loc7_:SWave = new SWave(_loc5_,_loc6_,_loc4_,0,_loc4_);
         if(this.friend)
         {
            InitData.TARGETS.swa1.addOne1(_loc7_);
         }
         else
         {
            InitData.TARGETS.swa2.addOne1(_loc7_);
         }
         var _loc8_:SoundUBlt32 = new SoundUBlt32(InitData.SOUNDDATA.soundArray[4],4,new Point(_loc5_,_loc6_));
         _loc8_.volMut = 1.5;
         InitData.SOUNDC32.addSFX25(_loc8_);
         var _loc9_:Number = this.part.part.partMove.hitRect.width + this.part.part.partMove.hitRect.height;
         InitData.CORE.cam.addShake(this.part.part.partMove.point.x,this.part.part.partMove.point.y,_loc9_ * 0.02);
         _loc5_ = this.part.part.partMove.hitRect.x + Math.random() * this.part.part.partMove.hitRect.width;
         _loc6_ = this.part.part.partMove.hitRect.y + Math.random() * this.part.part.partMove.hitRect.height;
         this.part.part.partMove.point.x = this.part.part.partMove.point.y = -1000000;
      }
   }
}

