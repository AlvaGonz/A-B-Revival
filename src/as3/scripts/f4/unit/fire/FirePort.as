package f4.unit.fire
{
   import flash.geom.Point;
   
   public class FirePort
   {
      
      public var point:Point;
      
      public var disRad:Point;
      
      public var fireModes:Array;
      
      public function FirePort(param1:Number, param2:Number)
      {
         super();
         this.point = new Point(param1,param2);
         this.disRad = new Point(Math.sqrt(param1 * param1 + param2 * param2),Math.atan2(param2,param1));
         this.fireModes = [];
      }
      
      public function runModes(param1:Boolean, param2:Number, param3:Point) : void
      {
         var _loc4_:FireMode = null;
         for each(_loc4_ in this.fireModes)
         {
            _loc4_.runMode(param1,param2,param3);
         }
      }
   }
}

