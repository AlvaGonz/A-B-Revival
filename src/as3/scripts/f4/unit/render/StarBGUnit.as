package f4.unit.render
{
   import f4.data.InitData;
   import f4.gfx.bmpdmc.BMPD2D;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class StarBGUnit
   {
      
      public var moveScale:Point;
      
      public var wh:Point;
      
      public var stars2d:Array;
      
      public var wh2d:Point;
      
      private var _ckrect1:Rectangle;
      
      private var _ckrect2:Rectangle;
      
      private var _rect2:Rect2;
      
      private var _maxi:int;
      
      private var _maxj:int;
      
      public function StarBGUnit(param1:Number, param2:Number, param3:Number, param4:Number, param5:Number, param6:Number)
      {
         var _loc7_:int = 0;
         var _loc8_:int = 0;
         super();
         this.wh = new Point(param1,param2);
         this.moveScale = new Point(param3,param4);
         this.wh2d = new Point(param5,param6);
         var _loc9_:int = Math.ceil(param1 / this.wh2d.x);
         var _loc10_:int = Math.ceil(param2 / this.wh2d.y);
         this._maxi = _loc9_;
         this._maxj = _loc10_;
         this.stars2d = [];
         _loc7_ = 0;
         while(_loc7_ <= _loc9_)
         {
            this.stars2d[_loc7_] = [];
            _loc8_ = 0;
            while(_loc8_ <= _loc10_)
            {
               this.stars2d[_loc7_][_loc8_] = [];
               _loc8_++;
            }
            _loc7_++;
         }
         this._ckrect1 = new Rectangle();
         this._ckrect2 = new Rectangle(0,0,InitData.RENDERDATA.W,InitData.RENDERDATA.H);
         this._rect2 = StaticShit.RECT2;
      }
      
      public function addBmpd2d(param1:BMPD2D, param2:Number, param3:Number, param4:Number, param5:Number) : void
      {
         var _loc13_:int = 0;
         var _loc14_:int = 0;
         var _loc6_:Number = (param2 * param4 - param1.bmpd.rectSmall.width / 2) / this.moveScale.x;
         var _loc7_:Number = (param3 * param5 - param1.bmpd.rectSmall.height / 2) / this.moveScale.y;
         var _loc8_:SStarUnit = new SStarUnit(param1,new Point(_loc6_,_loc7_));
         var _loc9_:int = _loc6_ / this.wh2d.x;
         var _loc10_:int = Math.ceil((_loc6_ + param1.bmpd.rectSmall.width / this.moveScale.x) / this.wh2d.x);
         var _loc11_:int = _loc7_ / this.wh2d.y;
         var _loc12_:int = Math.ceil((_loc7_ + param1.bmpd.rectSmall.height / this.moveScale.y) / this.wh2d.y);
         _loc13_ = _loc9_;
         while(_loc13_ <= _loc10_)
         {
            _loc14_ = _loc11_;
            while(_loc14_ <= _loc12_)
            {
               (this.stars2d[_loc13_][_loc14_] as Array).push(_loc8_);
               _loc14_++;
            }
            _loc13_++;
         }
      }
      
      public function renderFrom(param1:Number, param2:Number, param3:Number, param4:Number) : void
      {
         var _loc11_:int = 0;
         var _loc12_:int = 0;
         var _loc13_:SStarUnit = null;
         var _loc5_:Number = param3 / 2;
         var _loc6_:Number = param4 / 2;
         var _loc7_:int = (param1 + _loc5_ - (param3 + _loc5_) / this.moveScale.x) / this.moveScale.x / this.wh2d.x;
         var _loc8_:int = Math.ceil((param1 + _loc5_ + (param3 + _loc5_) / this.moveScale.x) / this.moveScale.x / this.wh2d.x);
         var _loc9_:int = (param2 + _loc6_ - (param4 + _loc6_) / this.moveScale.y) / this.moveScale.y / this.wh2d.y;
         var _loc10_:int = Math.ceil((param2 + _loc6_ + (param4 + _loc6_) / this.moveScale.y) / this.moveScale.y / this.wh2d.y);
         if(_loc7_ < 0)
         {
            _loc7_ = 0;
         }
         if(_loc8_ > this._maxi)
         {
            _loc8_ = this._maxi;
         }
         if(_loc9_ < 0)
         {
            _loc9_ = 0;
         }
         if(_loc10_ > this._maxj)
         {
            _loc10_ = this._maxj;
         }
         _loc11_ = _loc7_;
         while(_loc11_ <= _loc8_)
         {
            _loc12_ = _loc9_;
            while(_loc12_ <= _loc10_)
            {
               for each(_loc13_ in this.stars2d[_loc11_][_loc12_])
               {
                  if(_loc13_.renderFrame)
                  {
                     _loc13_.bmpd2d.bmpd.nextFrame();
                     _loc13_.bmpd2d.bmpd.moveRect();
                     _loc13_.bmpd2d.drawPoint.x = (_loc13_.point.x * this.moveScale.x - param1) * this.moveScale.x;
                     _loc13_.bmpd2d.drawPoint.y = (_loc13_.point.y * this.moveScale.y - param2) * this.moveScale.y;
                     this._ckrect1.x = _loc13_.bmpd2d.drawPoint.x;
                     this._ckrect1.y = _loc13_.bmpd2d.drawPoint.y;
                     this._ckrect1.width = _loc13_.bmpd2d.bmpd.rectSmall.width;
                     this._ckrect1.height = _loc13_.bmpd2d.bmpd.rectSmall.height;
                     if(this._rect2.rectIntersects(this._ckrect1,this._ckrect2))
                     {
                        _loc13_.bmpd2d.render(0);
                        _loc13_.renderFrame = false;
                     }
                  }
               }
               _loc12_++;
            }
            _loc11_++;
         }
         _loc11_ = _loc7_;
         while(_loc11_ <= _loc8_)
         {
            _loc12_ = _loc9_;
            while(_loc12_ <= _loc10_)
            {
               for each(_loc13_ in this.stars2d[_loc11_][_loc12_])
               {
                  _loc13_.renderFrame = true;
               }
               _loc12_++;
            }
            _loc11_++;
         }
      }
   }
}

