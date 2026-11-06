package fullshit.tool
{
   import flash.geom.Point;
   
   public class LineCP extends Line
   {
      
      public var a:Number;
      
      public var b:Number;
      
      public var c:Number;
      
      private var _fastCP:Point;
      
      public function LineCP(param1:Point, param2:Point)
      {
         super(param1,param2);
         this.a = p2.y - p1.y;
         this.b = p1.x - p2.x;
         this.c = p1.y * p2.x - p1.x * p2.y;
         this._fastCP = new Point(0,0);
      }
      
      public function init(param1:Point, param2:Point) : void
      {
         p1 = param1;
         p2 = param2;
         this.a = p2.y - p1.y;
         this.b = p1.x - p2.x;
         this.c = p1.y * p2.x - p1.x * p2.y;
         this._fastCP.x = this._fastCP.y = 0;
      }
      
      public function initXY(param1:Number, param2:Number, param3:Number, param4:Number) : void
      {
         p1.x = param1;
         p1.y = param2;
         p2.x = param3;
         p2.y = param4;
         this.a = p2.y - p1.y;
         this.b = p1.x - p2.x;
         this.c = p1.y * p2.x - p1.x * p2.y;
         this._fastCP.x = this._fastCP.y = 0;
      }
      
      public function pointToDistance(param1:EPoint) : Number
      {
         return Math.abs(this.a * param1.x + this.b * param1.y + this.c) / Math.sqrt(this.a * this.a + this.b * this.b);
      }
      
      public function crossPoint(param1:LineCP) : Point
      {
         if(!isCross(param1))
         {
            return null;
         }
         if(this.a * param1.b - this.b * param1.a < 0.001)
         {
            trace("same");
            return null;
         }
         this._fastCP.x = (param1.b * this.c - this.b * param1.c) / (param1.a * this.b - this.a * param1.b);
         this._fastCP.y = (this.a * param1.c - param1.a * this.c) / (param1.a * this.b - this.a * param1.b);
         return this._fastCP;
      }
   }
}

