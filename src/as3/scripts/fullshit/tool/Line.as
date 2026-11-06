package fullshit.tool
{
   import flash.geom.Point;
   
   public class Line
   {
      
      public var p1:Point;
      
      public var p2:Point;
      
      public function Line(param1:Point, param2:Point)
      {
         super();
         this.p1 = param1;
         this.p2 = param2;
      }
      
      public function get minX() : Number
      {
         if(this.p1.x > this.p2.x)
         {
            return this.p2.x;
         }
         return this.p1.x;
      }
      
      public function get maxX() : Number
      {
         if(this.p1.x > this.p2.x)
         {
            return this.p1.x;
         }
         return this.p2.x;
      }
      
      public function get minY() : Number
      {
         if(this.p1.y > this.p2.y)
         {
            return this.p2.y;
         }
         return this.p1.y;
      }
      
      public function get maxY() : Number
      {
         if(this.p1.y > this.p2.y)
         {
            return this.p1.y;
         }
         return this.p2.y;
      }
      
      public function get rad() : Number
      {
         return Math.atan2(this.p2.y - this.p1.y,this.p2.x - this.p1.x);
      }
      
      public function isCross(param1:Line) : Boolean
      {
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         _loc2_ = this.p1.x * (param1.p1.y - this.p2.y) + this.p2.x * (this.p1.y - param1.p1.y) + param1.p1.x * (this.p2.y - this.p1.y);
         _loc3_ = this.p1.x * (param1.p2.y - this.p2.y) + this.p2.x * (this.p1.y - param1.p2.y) + param1.p2.x * (this.p2.y - this.p1.y);
         if((_loc2_ ^ _loc3_) >= 0)
         {
            return false;
         }
         _loc2_ = param1.p1.x * (this.p1.y - param1.p2.y) + param1.p2.x * (param1.p1.y - this.p1.y) + this.p1.x * (param1.p2.y - param1.p1.y);
         _loc3_ = param1.p1.x * (this.p2.y - param1.p2.y) + param1.p2.x * (param1.p1.y - this.p2.y) + this.p2.x * (param1.p2.y - param1.p1.y);
         if((_loc2_ ^ _loc3_) >= 0)
         {
            return false;
         }
         return true;
      }
   }
}

