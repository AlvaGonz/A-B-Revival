package f4.data
{
   import f4.unit.ai.AIPart;
   import f4.unit.parts.SimpleFly;
   
   public class CkBltObj
   {
      
      public var hitPart:AIPart;
      
      public var blt:SimpleFly;
      
      public var fx:Number;
      
      public var fy:Number;
      
      public var short:int;
      
      public var step:Number;
      
      public var mvx:Number;
      
      public var mvy:Number;
      
      public var loop:int;
      
      public var hp2:Boolean;
      
      public function CkBltObj(param1:SimpleFly)
      {
         super();
         this.blt = param1;
      }
      
      public function updateCk() : void
      {
         this.short = 1000;
         this.hp2 = false;
         this.hitPart = null;
         this.step = this.blt.move.hitRect.width;
         this.mvx = Math.cos(this.blt.move.spd.y) * this.step;
         this.mvy = Math.sin(this.blt.move.spd.y) * this.step;
         this.loop = this.blt.move.spd.x / this.step;
      }
   }
}

