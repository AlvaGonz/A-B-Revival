package f4.unit.move
{
   import flash.geom.Point;
   
   public class Limiter
   {
      
      public var fNum:Number;
      
      public var dxy:Point;
      
      public var dspd:Point;
      
      public function Limiter(param1:Number = 0.5, param2:Number = 0, param3:Number = 0)
      {
         super();
         this.fNum = param1;
         this.dxy = new Point(param2,param3);
         this.dspd = new Point();
      }
      
      public function runLimiter() : void
      {
         this.dspd.x -= this.dxy.x * this.fNum;
         this.dspd.y -= this.dxy.y * this.fNum;
         this.dspd.x *= 0.5;
         this.dspd.y *= 0.5;
         this.dxy.x += this.dspd.x;
         this.dxy.y += this.dspd.y;
      }
   }
}

