package f4.data
{
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.tool.Line;
   
   public class HitRectPoints
   {
      
      public var line1:Array;
      
      public var line2:Array;
      
      public var xray:Line;
      
      public function HitRectPoints()
      {
         super();
         this.line1 = [new Line(new Point(),new Point()),new Line(new Point(),new Point()),new Line(new Point(),new Point()),new Line(new Point(),new Point())];
         this.line2 = [new Line(new Point(),new Point()),new Line(new Point(),new Point()),new Line(new Point(),new Point()),new Line(new Point(),new Point())];
         this.xray = new Line(new Point(),new Point(-1000));
      }
      
      public function rectHitPoints(param1:Point, param2:Array, param3:Rectangle) : Boolean
      {
         var _loc6_:Point = null;
         var _loc7_:Point = null;
         var _loc8_:Line = null;
         var _loc9_:Line = null;
         var _loc10_:Line = null;
         var _loc4_:int = -1;
         var _loc5_:int = 4;
         while(++_loc4_ < _loc5_ - 1)
         {
            _loc6_ = param2[_loc4_];
            _loc7_ = param2[_loc4_ + 1];
            _loc8_ = this.line1[_loc4_];
            _loc8_.p1.x = _loc6_.x + param1.x;
            _loc8_.p1.y = _loc6_.y + param1.y;
            _loc8_.p2.x = _loc7_.x + param1.x;
            _loc8_.p2.y = _loc7_.y + param1.y;
         }
         _loc6_ = param2[3];
         _loc7_ = param2[0];
         _loc8_ = this.line1[3];
         _loc8_.p1.x = _loc6_.x + param1.x;
         _loc8_.p1.y = _loc6_.y + param1.y;
         _loc8_.p2.x = _loc7_.x + param1.x;
         _loc8_.p2.y = _loc7_.y + param1.y;
         _loc8_ = this.line2[0];
         _loc8_.p1.x = param3.x;
         _loc8_.p1.y = param3.y;
         _loc8_.p2.x = param3.x + param3.width;
         _loc8_.p2.y = param3.y;
         _loc8_ = this.line2[1];
         _loc8_.p1.x = param3.x + param3.width;
         _loc8_.p1.y = param3.y;
         _loc8_.p2.x = param3.x + param3.width;
         _loc8_.p2.y = param3.y + param3.height;
         _loc8_ = this.line2[2];
         _loc8_.p1.x = param3.x + param3.width;
         _loc8_.p1.y = param3.y + param3.height;
         _loc8_.p2.x = param3.x;
         _loc8_.p2.y = param3.y + param3.height;
         _loc8_ = this.line2[3];
         _loc8_.p1.x = param3.x;
         _loc8_.p1.y = param3.y + param3.height;
         _loc8_.p2.x = param3.x;
         _loc8_.p2.y = param3.y;
         var _loc11_:int = 0;
         this.xray.p1.x = param3.x + param3.width / 2;
         this.xray.p2.y = this.xray.p1.y = param3.y + param3.height / 2;
         for each(_loc9_ in this.line1)
         {
            if(this.xray.isCross(_loc9_))
            {
               _loc11_++;
            }
         }
         if(_loc11_ % 2 != 0)
         {
            return true;
         }
         _loc11_ = 0;
         this.xray.p1.x = param1.x;
         this.xray.p2.y = this.xray.p1.y = param1.y;
         for each(_loc10_ in this.line2)
         {
            if(this.xray.isCross(_loc10_))
            {
               _loc11_++;
            }
         }
         if(_loc11_ % 2 != 0)
         {
            return true;
         }
         loop3:
         for each(_loc9_ in this.line1)
         {
            var _loc14_:int = 0;
            var _loc15_:* = this.line2;
            do
            {
               for each(_loc10_ in _loc15_)
               {
               }
               continue loop3;
            }
            while(!_loc9_.isCross(_loc10_));
            return true;
         }
         return false;
      }
   }
}

