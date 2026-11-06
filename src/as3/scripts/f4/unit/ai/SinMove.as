package f4.unit.ai
{
   import fullshit.tool.MathTool;
   import fullshit.tool.StaticShit;
   
   public class SinMove
   {
      
      public var x:Number;
      
      public var y:Number;
      
      private var _x:Number;
      
      private var _mtY:Number;
      
      private var _mtX:Number;
      
      private var _mathTool:MathTool;
      
      public function SinMove(param1:Number, param2:Number, param3:Number = 1.55)
      {
         super();
         this.x = this.y = 0;
         this._x = param3;
         this._mtY = param2;
         this._mtX = param1;
         this._mathTool = StaticShit.MATHTOOL;
      }
      
      public function runSin(param1:Number) : void
      {
         this._x += this._mtX;
         this._x = this._mathTool.formatRad(this._x);
         var _loc2_:Number = Math.sin(this._x) * this._mtY;
         var _loc3_:Number = Math.sin(param1);
         var _loc4_:Number = Math.cos(param1);
         this.x = -_loc2_ * _loc3_;
         this.y = _loc2_ * _loc4_;
      }
   }
}

