package f4.unit.outer
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.unit.ai.AIUnity;
   import f4.unit.fire.FireTimer;
   import f4.unit.particle.Particle;
   import f4data.F4DataLinks;
   import flash.geom.Point;
   import fullshit.ds.AbsOne;
   
   public class AIOuter extends AbsOne
   {
      
      public var timer:FireTimer;
      
      public var point:Point;
      
      public var aim:Point;
      
      public var linkData:F4DataLinks;
      
      public var max:int;
      
      public var limit:int;
      
      public var num:int;
      
      public var hue:int;
      
      public var pilots:Array;
      
      public var ignoreAI:Boolean;
      
      public var aidt:int;
      
      public var roundStop:Boolean = false;
      
      public function AIOuter(param1:Number, param2:Number, param3:Point, param4:int, param5:F4DataLinks, param6:Array, param7:int, param8:int, param9:int, param10:int, param11:int, param12:Boolean, param13:int)
      {
         super();
         this.ignoreAI = param12;
         this.linkData = param5;
         this.timer = new FireTimer(param9,param10,param11);
         this.point = new Point(param1,param2);
         this.aim = param3;
         this.max = param7;
         this.limit = param8;
         this.pilots = param6;
         this.aidt = param13;
         param5.colorHue = param4;
         this.hue = param4;
         trace(this.linkData.index);
         InitData.GDATA.f4linkSub(param5,param5);
      }
      
      override public function runA1() : void
      {
         var _loc1_:AIUnity = null;
         var _loc2_:Particle = null;
         var _loc3_:Targets = null;
         var _loc4_:int = 0;
         if(InitData.TARGETS.efa1.array.length < this.max)
         {
            if(!this.roundStop && this.timer.isFireFrame)
            {
               this.linkData.colorHue = this.hue;
               _loc1_ = InitData.MAKER.makeAIFlyByData(this.point.x,this.point.y,this.linkData,false);
               _loc1_.ignoreAI = this.ignoreAI;
               _loc1_.unity.move.autof = InitData.autof;
               _loc1_.initAI(this.aim);
               _loc1_.pilotName = this.pilots;
               _loc1_.aimode = InitData.AIDATA.ais[this.aidt];
               _loc1_.aimodeNum = this.aidt;
               InitData.TARGETS.efa1.addOne1(_loc1_);
               _loc1_.buildHitAll();
               _loc3_ = InitData.TARGETS;
               _loc4_ = 0;
               while(_loc4_ < 15)
               {
                  _loc2_ = new Particle(InitData.CORE.cam,Math.random() * _loc1_.unity.move.hitRect.width + _loc1_.unity.move.hitRect.x,Math.random() * _loc1_.unity.move.hitRect.height + _loc1_.unity.move.hitRect.y,Math.random() * Math.PI * 2,Math.random() * 0.8,1,0,0.999,Math.random() * 50 + 20);
                  _loc2_.layer = 1;
                  _loc2_.flash = true;
                  _loc2_.color = 16777215;
                  _loc3_.pa1.addOne1(_loc2_);
                  _loc4_++;
               }
               this.roundStop = true;
               ++this.num;
               if(this.num >= this.limit)
               {
                  isDeadA1 = true;
               }
            }
         }
      }
      
      override public function startRemoveA1() : void
      {
      }
   }
}

