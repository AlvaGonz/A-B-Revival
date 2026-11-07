package f4.unit.ai
{
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class AIRunMode
   {
      
      public var frameEx:int = 60;
      
      public var strong1:Rectangle = new Rectangle(0.25,1,1);
      
      public var strong2:Rectangle = new Rectangle(0.09,1,0.1);
      
      public var strong3:Point = new Point(2,1);
      
      public var weak1:Rectangle = new Rectangle(0.64,1,1);
      
      public var weak2:Rectangle = new Rectangle(0.35,2,0.5);
      
      public var weak3:Point = new Point(3,1);
      
      public var bodyRt2:Number = 3;
      
      public var bodyFar:Point = new Point(1,1);
      
      public var bodyClose:Point = new Point(2,0.5);
      
      public function AIRunMode()
      {
         super();
      }
   }
}

