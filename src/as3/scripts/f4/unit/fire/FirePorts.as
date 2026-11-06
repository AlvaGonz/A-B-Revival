package f4.unit.fire
{
   import flash.geom.Point;
   
   public class FirePorts
   {
      
      public var ports:Array;
      
      public function FirePorts()
      {
         super();
         this.ports = [];
      }
      
      public function runPorts(param1:Boolean, param2:Number, param3:Point) : void
      {
         var _loc4_:FirePort = null;
         for each(_loc4_ in this.ports)
         {
            _loc4_.runModes(param1,param2,param3);
         }
      }
   }
}

