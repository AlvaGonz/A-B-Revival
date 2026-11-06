package f4.unit.render
{
   import flash.display.BitmapData;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class StarBGUnitSimple
   {
      
      public var moveScale:Point;
      
      public var oxy:Point;
      
      public var wh:Point;
      
      public var stars:Array;
      
      public var drawRect:Rectangle;
      
      public function StarBGUnitSimple(param1:Number, param2:Number, param3:Number, param4:Number, param5:Number, param6:Number)
      {
         super();
         this.oxy = new Point(param1,param2);
         this.wh = new Point(param3,param4);
         this.stars = [];
         this.moveScale = new Point(param5,param6);
         this.drawRect = new Rectangle();
      }
      
      public function initStars(param1:int, param2:Number, param3:uint, param4:uint, param5:uint) : void
      {
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:int = 0;
         while(_loc8_ < param1)
         {
            _loc6_ = Math.random() * this.wh.x;
            _loc7_ = Math.random() * this.wh.y;
            this.stars.push(new StarUnit(new Rectangle(_loc6_,_loc7_,param2,param2),uint(param4 + Math.random() * param5) << 24 | param3));
            _loc8_++;
         }
      }
      
      public function renderFrom(param1:BitmapData, param2:Number, param3:Number) : void
      {
         var _loc4_:StarUnit = null;
         var _loc5_:Number = param2;
         var _loc6_:Number = param3;
         for each(_loc4_ in this.stars)
         {
            _loc4_.rect.x -= _loc5_;
            _loc4_.rect.y -= _loc6_;
            while(_loc4_.rect.x < 0)
            {
               _loc4_.rect.x += this.wh.x;
            }
            while(_loc4_.rect.x > this.wh.x)
            {
               _loc4_.rect.x -= this.wh.x;
            }
            while(_loc4_.rect.y < 0)
            {
               _loc4_.rect.y += this.wh.y;
            }
            while(_loc4_.rect.y > this.wh.y)
            {
               _loc4_.rect.y -= this.wh.y;
            }
            this.drawRect.x = (_loc4_.rect.x - this.oxy.x) * this.moveScale.x;
            this.drawRect.y = (_loc4_.rect.y - this.oxy.y) * this.moveScale.y;
            this.drawRect.width = _loc4_.rect.width;
            this.drawRect.height = _loc4_.rect.height;
            param1.fillRect(this.drawRect,_loc4_.color);
         }
      }
   }
}

