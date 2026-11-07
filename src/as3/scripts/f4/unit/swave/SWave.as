package f4.unit.swave
{
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.ds.AbsOne;
   
   public class SWave extends AbsOne
   {
      
      public var point:Point;
      
      public var r:Number;
      
      public var pow:Number;
      
      public var autof:Number;
      
      public var rAdd:Number;
      
      public var rf:Number;
      
      public var rect:Rectangle;
      
      public var alpha:int = 255;
      
      public function SWave(param1:Number, param2:Number, param3:Number, param4:Number, param5:Number, param6:Number = 0.75, param7:Number = 0.65)
      {
         super();
         this.point = new Point(param1,param2);
         this.r = param4;
         this.rAdd = param5;
         this.rf = param6;
         this.pow = param3;
         this.autof = param7;
         this.rect = new Rectangle();
      }
      
      override public function runA1() : void
      {
         this.r += this.rAdd;
         this.rect.x = this.point.x - this.r;
         this.rect.y = this.point.y - this.r;
         this.rect.width = this.r * 2;
         this.rect.height = this.r * 2;
         this.pow *= this.autof;
         this.rAdd *= this.rf;
         if(this.pow < 0.1)
         {
            isDeadA1 = true;
         }
         this.alpha = 10 / this.pow * 255;
         if(this.alpha > 255)
         {
            this.alpha = 255;
         }
      }
      
      override public function startRemoveA1() : void
      {
         this.point = null;
         this.rect = null;
      }
   }
}

