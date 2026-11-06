package fullshit.tool
{
   public class EPoint
   {
      
      protected var _x:Number;
      
      protected var _y:Number;
      
      protected var _rad:Number;
      
      protected var _dis:Number;
      
      protected var _isMove:Boolean;
      
      private var _rotx:Number;
      
      private var _roty:Number;
      
      private const PI180:Number = 57.29577951308232;
      
      private var _mathTool:MathTool;
      
      public function EPoint(param1:Number, param2:Number, param3:Number = 0, param4:Number = 0)
      {
         super();
         this._rotx = param3;
         this._roty = param4;
         this._x = param1;
         this._y = param2;
         this._isMove = true;
         this._mathTool = StaticShit.MATHTOOL;
         this.makeDis();
      }
      
      public function changeRotP(param1:Number, param2:Number) : void
      {
         this._rotx = param1;
         this._roty = param2;
         this._isMove = true;
      }
      
      public function isSame(param1:EPoint) : Boolean
      {
         if(param1.x == this._x && param1.y == this._y)
         {
            return true;
         }
         return false;
      }
      
      public function rot(param1:Number) : void
      {
         this._rad = this.rad + param1 / this.PI180;
         this._rotTo();
      }
      
      public function rotRad(param1:Number) : void
      {
         this._rad = this.rad + param1;
         this._rotTo();
      }
      
      public function rotToRad(param1:Number) : void
      {
         this._rad = param1;
         this._rotTo();
      }
      
      public function makeDis() : void
      {
         if(this._isMove)
         {
            this._isMove = false;
            this._rad = Math.atan2(this._y - this._roty,this._x - this._rotx);
            this._dis = Math.sqrt(Math.pow(this._x - this._rotx,2) + Math.pow(this._y - this._roty,2));
         }
      }
      
      public function rotTo(param1:Number) : void
      {
         this._rad = param1 / this.PI180;
         this._rotTo();
      }
      
      public function get ang() : Number
      {
         return this.rad * this.PI180;
      }
      
      public function get rad() : Number
      {
         this.makeDis();
         return this._rad;
      }
      
      public function get dis() : Number
      {
         this.makeDis();
         return this._dis;
      }
      
      public function get x() : Number
      {
         return this._x;
      }
      
      public function get y() : Number
      {
         return this._y;
      }
      
      public function set x(param1:Number) : void
      {
         this._x = param1;
         this._isMove = true;
      }
      
      public function set y(param1:Number) : void
      {
         this._y = param1;
         this._isMove = true;
      }
      
      public function copyFrom(param1:EPoint) : void
      {
         this._dis = param1.dis;
         this._rad = param1.rad;
         this._isMove = false;
         this._rotTo();
      }
      
      public function scale(param1:Number) : void
      {
         this._x *= param1;
         this._y *= param1;
         this._isMove = true;
      }
      
      public function addDis(param1:Number) : void
      {
         this._dis = this.dis + param1;
         if(this._dis < 0)
         {
            this._dis = -this._dis;
         }
         if(this._isMove)
         {
            this._isMove = false;
            this._rad = Math.atan2(this._y - this._roty,this._x - this._rotx);
         }
         this._rotTo();
      }
      
      public function disTo(param1:Number) : void
      {
         this._dis = param1;
         if(this._dis < 0)
         {
            this._dis = -this._dis;
            this.scale(-1);
         }
         if(this._isMove)
         {
            this._isMove = false;
            this._rad = Math.atan2(this._y - this._roty,this._x - this._rotx);
         }
         this._rotTo();
      }
      
      private function _rotTo() : void
      {
         this.makeDis();
         this._rad = this._mathTool.formatRad(this._rad);
         this._x = Math.cos(this._rad) * this._dis + this._rotx;
         this._y = Math.sin(this._rad) * this._dis + this._roty;
      }
   }
}

