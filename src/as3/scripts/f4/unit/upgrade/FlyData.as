package f4.unit.upgrade
{
   import flash.geom.Point;
   
   public class FlyData
   {
      
      public var hp:Point;
      
      public var movepow:Point;
      
      public var turn:Point;
      
      public var fireacc:Number;
      
      public var mass:Number;
      
      public var fireRange:Number;
      
      public var firePow:Number;
      
      public var fireSpd:Number;
      
      public var haveTurn:Boolean;
      
      public var name:String;
      
      public var type:int;
      
      public function FlyData(param1:String, param2:Point, param3:Point, param4:Point, param5:Number, param6:Number)
      {
         super();
         this.name = param1;
         this.hp = param2;
         this.movepow = param3;
         this.turn = param4;
         this.fireacc = param5;
         this.mass = param6;
         this.fireSpd = this.firePow = this.fireRange = 0;
         this.haveTurn = false;
      }
   }
}

